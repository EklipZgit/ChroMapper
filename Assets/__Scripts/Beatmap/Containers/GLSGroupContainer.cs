using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class GLSGroupContainer : ObjectContainer
    {
        // Match the dithered transparency property used by passed note models.
        private static readonly int alwaysTranslucentId = Shader.PropertyToID("_AlwaysTranslucent");
        private static readonly int translucentAlphaId = Shader.PropertyToID("_TranslucentAlpha");
        // Keep the shared preview pool with the other static state.
        private static readonly Stack<GLSGroupContainer> previewGhostPool = new();

        [SerializeField] public VisualModelController VModelController;
        [SerializeField] private GLSGroupAppearanceSO glsGroupAppearance;
        [SerializeField] private TracksManager tracksManager;
        [SerializeField] private TextMeshPro[] valueDisplays;
        [SerializeField] private GLSEventIconView iconView;
        [SerializeField] public LightGradientController lightGradientController;
        [SerializeField] private LightGradientController incomingLightGradientController;
        public LightGradientController IncomingLightGradientController => incomingLightGradientController;
        // PR 666 renamed the track-definition asset; the GLS previews must use the compatible authoritative type.
        [SerializeField] public TrackDefinitionsSO TrackDefinitions;

        public BaseEventBoxGroup EventBoxGroupData;

        // Retain the represented inner node so outer-track ghost interactions can target it later.
        public BaseGLSEvent PreviewEventData;

        // Track dynamically-created previews so a recycled group container can rebuild them safely.
        private readonly List<GLSGroupContainer> previewGhosts = new();

        private readonly Dictionary<BaseGLSEvent, GLSGroupContainer> previewGhostByEvent = new();
        private readonly HashSet<GLSGroupContainer> retainedPreviewGhosts = new();
        private readonly List<GLSGroupContainer> configuredPreviewGhosts = new();
        private readonly HashSet<BaseGLSEvent> desiredPreviewEventSet = new();
        private readonly List<BaseGLSEvent> desiredPreviewEvents = new();

        private PreviewConfigurationStage previewConfigurationStage;
        private int previewConfigurationEventIndex;
        private int previewConfigurationReleaseIndex;
        private float previewConfigurationPreviousOffset;
        private bool previewConfigurationForceAppearanceRefresh;
        private bool previewConfigurationUsesGroupAppearance;
        private BaseGLSEvent configuredPrimaryPreviewEvent;

        // ScrollingShowsEveryGlsNodeBeforeRibbonWork keeps node binding synchronous while color ribbon uploads wait for spare frame time.
        private bool deferPreviewRibbons;
        private bool previewRibbonDirty;
        private int pendingPreviewRibbonCount;
        private int nextPreviewRibbonIndex;

        private Transform previewGhostRoot;
        // OuterGhostPreviewShrinkPreservesColorRibbonLength: ghost geometry has its
        // own scaled parent while timeline ribbons remain on the unscaled owner.
        private Transform previewVisualRoot;

        // Retain the boost lookup so existing source nodes can refresh ribbons after a later target changes easing.
        private Func<float, bool> previewBoostResolver;

        // Distinguish the collection-owned node from its translucent, dynamically-created previews.
        private bool isPreviewGhost;

        // Rebinding keeps preview slots stable under the pointer while staged configuration reuses existing capacity.
        private bool preservePreviewSlotsOnNextConfigure;
        private bool reusePreviewCapacityOnNextConfigure;
        private bool previewSlotsConfigured;
        // GLSScrolling rebinds this pooled owner in the same refresh without unregistering its text meshes.
        internal bool HasActivePooledPreviewRoot { get; private set; }

        // Every ghost visual shrinks by the same global factor, so cache the child scale.
        private static float cachedInnerPreviewShrink = float.NaN;
        private static Vector3 cachedInnerPreviewVisualScale;

        // Let a hovered pooled preview update the visual outline of its owning logical group.
        private GLSGroupContainer previewOwner;

        private enum PreviewConfigurationStage
        {
            None,
            HideReboundRoot,
            ReparentReboundRoot,
            ConfigurePrimary,
            ConfigureGhosts,
            ReleaseGhosts,
            ActivateReboundRoot,
            SuspendRoot,
        }

        // Queue priority follows the next rendered node, so multiple GLS groups materialize in ascending song time.
        internal float PreviewConfigurationPrioritySongBpmTime
        {
            get
            {
                // After scrub nodes finish, the queue follows the next still-dirty ribbon rather than the group's last node.
                if (previewConfigurationStage == PreviewConfigurationStage.None && pendingPreviewRibbonCount > 0)
                {
                    for (var index = nextPreviewRibbonIndex; index <= previewGhosts.Count; index++)
                    {
                        var preview = index == 0 ? this : previewGhosts[index - 1];
                        if (preview.previewRibbonDirty && preview.PreviewEventData != null)
                            return preview.PreviewEventData.SongBpmTime;
                    }
                }

                switch (previewConfigurationStage)
                {
                    case PreviewConfigurationStage.HideReboundRoot:
                    case PreviewConfigurationStage.ReparentReboundRoot:
                    case PreviewConfigurationStage.ConfigurePrimary:
                        return PreviewEventData != null
                            ? PreviewEventData.SongBpmTime
                            : EventBoxGroupData != null
                                ? EventBoxGroupData.SongBpmTime
                                : float.PositiveInfinity;
                    case PreviewConfigurationStage.ConfigureGhosts:
                        for (var index = previewConfigurationEventIndex; index < desiredPreviewEvents.Count; index++)
                        {
                            var previewEvent = desiredPreviewEvents[index];
                            if (!Mathf.Approximately(
                                previewEvent.RelativeJsonTime,
                                previewConfigurationPreviousOffset))
                            {
                                return previewEvent.SongBpmTime;
                            }
                        }
                        break;
                    case PreviewConfigurationStage.SuspendRoot:
                        return float.PositiveInfinity;
                }

                if (desiredPreviewEvents.Count > 0)
                {
                    return desiredPreviewEvents[desiredPreviewEvents.Count - 1].SongBpmTime;
                }
                return PreviewEventData != null
                    ? PreviewEventData.SongBpmTime
                    : EventBoxGroupData != null
                        ? EventBoxGroupData.SongBpmTime
                        : float.PositiveInfinity;
            }
        }

        // Resolve ghost-node drags to the collection-owned group so Alt-drag moves every node together.
        public GLSGroupContainer DragTarget => previewOwner != null ? previewOwner : this;

        private bool groupDragActive;
        private bool groupWasSelectedBeforeDrag;

        // Keep outer-track GLS previews visually selected whenever their one logical group is selected.
        public override bool Selected
        {
            get => base.Selected;
            set
            {
                // Propagate even when the primary value is unchanged because previews may have just been rebuilt.
                base.Selected = value;
                if (isPreviewGhost) return;

                foreach (var previewGhost in previewGhosts) previewGhost.Selected = value;
            }
        }

        public override BaseObject ObjectData
        {
            get => EventBoxGroupData;
            set
            {
                if (!ReferenceEquals(EventBoxGroupData, value) && value == null)
                {
                    reusePreviewCapacityOnNextConfigure = true;
                    previewSlotsConfigured = false;
                }

                EventBoxGroupData = (BaseEventBoxGroup)value;
                PreviewEventData = null;
            }
        }

        internal void RebindEventBoxGroup(BaseEventBoxGroup eventBoxGroup)
        {
            EventBoxGroupData = eventBoxGroup;
            PreviewEventData = null;
            preservePreviewSlotsOnNextConfigure = true;
            foreach (var previewGhost in previewGhosts)
            {
                previewGhost.EventBoxGroupData = eventBoxGroup;
            }
        }

        protected override void RegisterCallback()
        {
            if (isPreviewGhost) return;

            VisualSettings.OnBlockModelChanged += HandleModelChanged;
            VisualSettings.OnEventModelChanged += HandleModelChanged;
            SelectionController.OnSelectionChanged += SyncPreviewSelection;
        }

        protected override void UnregisterCallback()
        {
            if (isPreviewGhost)
                return;

            if (previewGhostRoot != null)
            {
                previewConfigurationStage = PreviewConfigurationStage.None;
                previewGhosts.Clear();
                previewGhostByEvent.Clear();
                retainedPreviewGhosts.Clear();
                configuredPreviewGhosts.Clear();
                desiredPreviewEventSet.Clear();
                desiredPreviewEvents.Clear();
                Destroy(previewGhostRoot.gameObject);
                previewGhostRoot = null;
            }

            VisualSettings.OnBlockModelChanged -= HandleModelChanged;
            VisualSettings.OnEventModelChanged -= HandleModelChanged;
            SelectionController.OnSelectionChanged -= SyncPreviewSelection;
        }

        // Update preview outlines only when selection changes, keeping ghost nodes free of per-frame work.
        private void SyncPreviewSelection()
        {
            var selected = EventBoxGroupData != null && SelectionController.IsObjectSelected(EventBoxGroupData);
            Selected = selected;
        }

        // Highlight every preview outline while keeping the group as the sole logical selection.
        public void SetGroupHighlighted(bool highlighted)
        {
            var owner = previewOwner != null ? previewOwner : this;
            owner.Highlighted = highlighted;
            foreach (var previewGhost in owner.previewGhosts) previewGhost.Highlighted = highlighted;
        }

        // Keep the whole logical GLS group blue while any one of its rendered nodes is being dragged.
        public void SetGroupDragged(bool dragged)
        {
            var owner = previewOwner != null ? previewOwner : this;
            if (dragged)
            {
                if (!owner.groupDragActive)
                {
                    owner.groupWasSelectedBeforeDrag = owner.Selected;
                    owner.groupDragActive = true;
                }

                // Use the normal selected outline color (blue in the editor) instead of the generic white drag outline.
                owner.Selected = true;
            }
            else if (owner.groupDragActive)
            {
                owner.Selected = owner.groupWasSelectedBeforeDrag;
                owner.groupDragActive = false;
            }

            owner.Dragged = dragged;
            foreach (var previewGhost in owner.previewGhosts)
                previewGhost.Dragged = dragged;
        }

        private void HandleModelChanged() => VModelController.Set(VisualSettings.GetBlockModel());

        public override void Setup() => DisablePassedObjectDither();

        public static GLSGroupContainer SpawnGLSGroup(
            BaseEventBoxGroup data,
            TrackDefinitionsSO trackDefinitions,
            ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<GLSGroupContainer>();
            container.EventBoxGroupData = data;
            // Preserve the GLS incoming transition ribbon while assigning PR 666's renamed track definitions.
            container.TrackDefinitions = trackDefinitions;
            container.incomingLightGradientController = Instantiate(
                container.lightGradientController, container.lightGradientController.transform.parent);
            container.incomingLightGradientController.name = "Incoming Color Transition Ribbon";
            container.incomingLightGradientController.gameObject.SetActive(false);
            return container;
        }

        public override void UpdateGridPosition()
        {
            var pos = transform.localPosition;
            // Keep every inner GLS node grounded after its shared 75%-scale appearance is applied. Fixes GLS nodes hovering too high above grid and being hard to tell where they are visually.
            pos.y = BeatmapConstant.EventNodeGroundedCenterY
                - ((EventAppearanceSO.FinalNodeScale - transform.localScale.y) / 2f);
            // Unity preview events need explicit null checks before choosing the rendered beat position.
            var previewSongBpmTime = PreviewEventData != null
                ? PreviewEventData.SongBpmTime
                : EventBoxGroupData.SongBpmTime;
            pos.z = previewSongBpmTime * EditorScaleController.EditorScale;
            transform.localPosition = pos;
            UpdateCollisionGroups();

            if (!isPreviewGhost && previewSlotsConfigured)
                foreach (var previewGhost in previewGhosts)
                    previewGhost.UpdateGridPosition();
        }

        // Render one selectable outer-track node per distinct inner-event offset.
        public void ConfigurePreviewNodes(Func<float, bool> isBoostAt)
            => ConfigurePreviewNodes(
                isBoostAt,
                float.NegativeInfinity,
                float.PositiveInfinity,
                null,
                true);

        // GLSScrolling rechecked more than 2,800 groups; compare the authoritative visible range with the last bound identities before rebuilding preview dictionaries.
        public bool HasSamePreviewNodeWindow(float lowerBound, float upperBound, ISet<BaseGLSEvent> retainedEvents)
        {
            if (EventBoxGroupData == null
                || !EventBoxGroupData.OrderedEventsInitialized
                || !previewSlotsConfigured
                || previewConfigurationStage != PreviewConfigurationStage.None)
            {
                return false;
            }

            var orderedEvents = EventBoxGroupData.OrderedEvents;
            var span = orderedEvents.AsSpan();
            var startIndex = span.LowerBoundBy(lowerBound, previewEvent => previewEvent.SongBpmTime);
            var endIndex = span.UpperBoundBy(upperBound, previewEvent => previewEvent.SongBpmTime);
            var desiredCount = endIndex - startIndex;
            for (var index = startIndex; index < endIndex; index++)
            {
                if (!desiredPreviewEventSet.Contains(orderedEvents[index]))
                    return false;
            }

            if (retainedEvents != null)
            {
                foreach (var retainedEvent in retainedEvents)
                {
                    if (!ReferenceEquals(retainedEvent.EventBoxGroupData, EventBoxGroupData))
                        continue;
                    if (!desiredPreviewEventSet.Contains(retainedEvent))
                        return false;
                    if (retainedEvent.SongBpmTime < lowerBound || retainedEvent.SongBpmTime > upperBound)
                        desiredCount++;
                }
            }

            return desiredCount == desiredPreviewEventSet.Count;
        }

        public void ConfigurePreviewNodes(
            Func<float, bool> isBoostAt,
            float lowerBound,
            float upperBound,
            ISet<BaseGLSEvent> retainedEvents,
            bool forceAppearanceRefresh = false)
        {
            BeginPreviewNodeConfiguration(
                isBoostAt,
                lowerBound,
                upperBound,
                retainedEvents,
                forceAppearanceRefresh,
                false);
            while (!ProcessPreviewNodeConfigurationStep())
            {
            }
            // A direct refresh must complete ribbons left pending by a previous scrolling frame.
            while (!ProcessNextPreviewRibbon())
            {
            }
        }

        public void BeginPreviewNodeConfiguration(
            Func<float, bool> isBoostAt,
            float lowerBound,
            float upperBound,
            ISet<BaseGLSEvent> retainedEvents,
            bool forceAppearanceRefresh,
            bool hideUntilConfigured,
            bool deferRibbonUpdate = false)
        {
            // A same-refresh replacement retains its active root only until the new visible nodes are bound.
            HasActivePooledPreviewRoot = false;
            // Preserve the collection's boost resolver for targeted ribbon-only refreshes that do not rebuild hover objects.
            previewBoostResolver = isBoostAt;
            // ScrollingShowsEveryGlsNodeBeforeRibbonWork defers only the costly color transition upload, never node appearance.
            deferPreviewRibbons = deferRibbonUpdate;
            nextPreviewRibbonIndex = 0;
            retainedPreviewGhosts.Clear();
            configuredPreviewGhosts.Clear();
            previewGhostByEvent.Clear();
            desiredPreviewEventSet.Clear();
            desiredPreviewEvents.Clear();
            foreach (var previewGhost in previewGhosts)
            {
                if (previewGhost.PreviewEventData != null)
                    previewGhostByEvent[previewGhost.PreviewEventData] = previewGhost;
            }

            previewConfigurationEventIndex = 0;
            previewConfigurationReleaseIndex = -1;
            previewConfigurationForceAppearanceRefresh = forceAppearanceRefresh;
            previewConfigurationUsesGroupAppearance = false;

            if (hideUntilConfigured)
            {
                gameObject.SetActive(false);
            }

            if (EventBoxGroupData == null)
            {
                previewConfigurationStage = PreviewConfigurationStage.ReleaseGhosts;
                previewConfigurationReleaseIndex = previewGhosts.Count - 1;
                return;
            }

            // Zero opacity restores the original single-node rendering path without creating ghost objects.
            if (Mathf.Approximately(Settings.Instance.GLSOuterTrackGhostNodeOpacity, 0f))
            {
                PreviewEventData = null;
                previewConfigurationUsesGroupAppearance = true;
                previewConfigurationStage = hideUntilConfigured && previewGhostRoot != null
                    ? PreviewConfigurationStage.HideReboundRoot
                    : PreviewConfigurationStage.ConfigurePrimary;
                return;
            }

            // Preview rebuilds can happen repeatedly while scrolling, so consume the maintained ordering unless it is uninitialized.
            if (!EventBoxGroupData.OrderedEventsInitialized)
                EventBoxGroupData.ResortOrderedEvents();

            // Unsupported group families have no outer preview nodes to reconcile.
            if (EventBoxGroupData is not (BaseLightColorEventBoxGroup or ILightTransformEventBoxGroup or BaseVfxEventEventBoxGroup))
            {
                previewConfigurationStage = PreviewConfigurationStage.None;
                if (hideUntilConfigured)
                    gameObject.SetActive(true);
                return;
            }

            var orderedEvents = EventBoxGroupData.OrderedEvents;
            if (orderedEvents.Count == 0)
            {
                PreviewEventData = null;
                previewConfigurationReleaseIndex = previewGhosts.Count - 1;
                previewConfigurationStage = hideUntilConfigured && previewGhostRoot != null
                    ? PreviewConfigurationStage.HideReboundRoot
                    : PreviewConfigurationStage.ReleaseGhosts;
                return;
            }

            // Range lookup is logarithmic in the complete group size.
            // Only visible nodes and indexed crossing ribbons enter the per-preview configuration loop on every playback chunk boundary.
            var span = orderedEvents.AsSpan();
            var startIndex = span.LowerBoundBy(lowerBound, previewEvent => previewEvent.SongBpmTime);
            var endIndex = span.UpperBoundBy(upperBound, previewEvent => previewEvent.SongBpmTime);
            for (var eventIndex = startIndex; eventIndex < endIndex; eventIndex++)
            {
                var previewEvent = orderedEvents[eventIndex];
                if (desiredPreviewEventSet.Add(previewEvent))
                    desiredPreviewEvents.Add(previewEvent);
            }
            if (retainedEvents != null)
            {
                foreach (var retainedEvent in retainedEvents)
                {
                    if (ReferenceEquals(retainedEvent.EventBoxGroupData, EventBoxGroupData)
                        && desiredPreviewEventSet.Add(retainedEvent))
                    {
                        desiredPreviewEvents.Add(retainedEvent);
                    }
                }
            }
            desiredPreviewEvents.Sort();

            var primaryPreviewEvent = orderedEvents[0];
            previewConfigurationForceAppearanceRefresh = forceAppearanceRefresh
                || hideUntilConfigured
                || !ReferenceEquals(configuredPrimaryPreviewEvent, primaryPreviewEvent);
            PreviewEventData = primaryPreviewEvent;
            previewConfigurationPreviousOffset = primaryPreviewEvent.RelativeJsonTime;
            previewConfigurationStage = hideUntilConfigured && previewGhostRoot != null
                ? PreviewConfigurationStage.HideReboundRoot
                : PreviewConfigurationStage.ConfigurePrimary;
        }

        public bool ProcessPreviewNodeConfigurationStep()
        {
            while (previewConfigurationStage != PreviewConfigurationStage.None)
            {
                switch (previewConfigurationStage)
                {
                    case PreviewConfigurationStage.HideReboundRoot:
                        previewGhostRoot.gameObject.SetActive(false);
                        previewConfigurationStage = PreviewConfigurationStage.ReparentReboundRoot;
                        return false;
                    case PreviewConfigurationStage.ReparentReboundRoot:
                        SetPreviewParent(transform.parent);
                        previewConfigurationStage = PreviewConfigurationStage.ConfigurePrimary;
                        return false;
                    case PreviewConfigurationStage.ConfigurePrimary:
                        previewConfigurationStage = PreviewConfigurationStage.ConfigureGhosts;
                        // Recycled collection owners must reclaim the solid primary role before any appearance is applied.
                        isPreviewGhost = false;
                        if (previewConfigurationUsesGroupAppearance)
                        {
                            ConfigureAsPreviewGhost(
                                previewBoostResolver(EventBoxGroupData.JsonTime),
                                previewBoostResolver,
                                deferPreviewRibbons);
                            configuredPrimaryPreviewEvent = null;
                            return false;
                        }
                        if (PreviewEventData != null && previewConfigurationForceAppearanceRefresh)
                        {
                            ConfigureAsPreviewGhost(
                                previewBoostResolver(PreviewEventData.JsonTime),
                                previewBoostResolver,
                                deferPreviewRibbons);
                            configuredPrimaryPreviewEvent = PreviewEventData;
                            return false;
                        }
                        break;
                    case PreviewConfigurationStage.ConfigureGhosts:
                        if (ProcessNextPreviewGhost())
                            return false;
                        previewConfigurationReleaseIndex = previewGhosts.Count - 1;
                        previewConfigurationStage = PreviewConfigurationStage.ReleaseGhosts;
                        break;
                    case PreviewConfigurationStage.ReleaseGhosts:
                        while (previewConfigurationReleaseIndex >= 0)
                        {
                            var previewGhost = previewGhosts[previewConfigurationReleaseIndex--];
                            if (retainedPreviewGhosts.Contains(previewGhost))
                                continue;
                            previewGhosts.Remove(previewGhost);
                            ReleasePreviewGhost(previewGhost);
                            return false;
                        }
                        previewGhosts.Clear();
                        previewGhosts.AddRange(configuredPreviewGhosts);
                        previewSlotsConfigured = true;
                        preservePreviewSlotsOnNextConfigure = false;
                        reusePreviewCapacityOnNextConfigure = false;
                        previewConfigurationStage = PreviewConfigurationStage.ActivateReboundRoot;
                        break;
                    case PreviewConfigurationStage.ActivateReboundRoot:
                        if (previewGhostRoot != null && !previewGhostRoot.gameObject.activeSelf)
                        {
                            previewGhostRoot.gameObject.SetActive(true);
                            previewConfigurationStage = PreviewConfigurationStage.None;
                            gameObject.SetActive(true);
                            SyncPreviewSelection();
                            return true;
                        }
                        previewConfigurationStage = PreviewConfigurationStage.None;
                        gameObject.SetActive(true);
                        SyncPreviewSelection();
                        break;
                    case PreviewConfigurationStage.SuspendRoot:
                        SuspendPreviewGhosts();
                        return true;
                }
            }

            return true;
        }

        private bool ProcessNextPreviewGhost()
        {
            while (previewConfigurationEventIndex < desiredPreviewEvents.Count)
            {
                var previewEvent = desiredPreviewEvents[previewConfigurationEventIndex++];
                if (previewEvent.RelativeJsonTime == previewConfigurationPreviousOffset)
                    continue;
                previewConfigurationPreviousOffset = previewEvent.RelativeJsonTime;

                var eventChanged = !previewGhostByEvent.TryGetValue(previewEvent, out var ghost);
                var recycledSlot = false;
                if (eventChanged && preservePreviewSlotsOnNextConfigure)
                {
                    // Same-offset replacement keeps the collider currently under the pointer physically stable.
                    ghost = previewGhosts.Find(candidate =>
                        !retainedPreviewGhosts.Contains(candidate)
                        && candidate.PreviewEventData != null
                        && Mathf.Approximately(
                            candidate.PreviewEventData.RelativeJsonTime,
                            previewEvent.RelativeJsonTime));
                }
                if (ghost == null && reusePreviewCapacityOnNextConfigure)
                {
                    // The 18-55-20 playback capture spent 1.49 seconds releasing ghosts; reuse any remaining
                    // inactive slot when a pooled parent changes owner instead of round-tripping through the stack.
                    ghost = previewGhosts.Find(candidate => !retainedPreviewGhosts.Contains(candidate));
                    recycledSlot = ghost != null;
                }
                if (ghost == null)
                {
                    ghost = GetPreviewGhost();
                    previewGhosts.Add(ghost);
                }

                retainedPreviewGhosts.Add(ghost);
                configuredPreviewGhosts.Add(ghost);
                previewGhostByEvent[previewEvent] = ghost;
                ghost.BindPreviewState(this, EventBoxGroupData, previewEvent, GlsLightCount, recycledSlot);
                if (eventChanged || previewConfigurationForceAppearanceRefresh)
                {
                    // Evaluate boost at this inner event's absolute time, not at the group's start time.
                    ghost.ConfigureAsPreviewGhost(
                        previewBoostResolver(previewEvent.JsonTime),
                        previewBoostResolver,
                        deferPreviewRibbons);
                    return true;
                }
            }

            return false;
        }

        // Refresh forward-owned ribbons on this group and its ghosts without recycling the nodes under the cursor.
        public void RefreshTransitionRibbons()
        {
            if (previewBoostResolver == null)
                return;
            // A direct edit refresh fulfills any older scrub ribbon job for the same visible node.
            glsGroupAppearance.UpdateTransitionRibbon(this, previewBoostResolver);
            ClearPreviewRibbonDirty();
            foreach (var previewGhost in previewGhosts)
            {
                glsGroupAppearance.UpdateTransitionRibbon(previewGhost, previewBoostResolver);
                previewGhost.ClearPreviewRibbonDirty();
            }
        }

        // Targeted variant of RefreshTransitionRibbons: only previews whose authored node appears in
        // the changed set — or whose same-time aggregate rewired — pay the ribbon rebuild.
        public void RefreshTransitionRibbons(
            HashSet<BaseLightColorBase> changedNodes,
            Dictionary<BaseEventBoxGroup, HashSet<float>> changedAggregates)
        {
            if (previewBoostResolver == null)
                return;
            if (TransitionRibbonPreviewChanged(PreviewEventData, changedNodes, changedAggregates))
            {
                // A targeted edit refresh also completes a queued scrub upload for this node.
                glsGroupAppearance.UpdateTransitionRibbon(this, previewBoostResolver);
                ClearPreviewRibbonDirty();
            }
            foreach (var previewGhost in previewGhosts)
            {
                if (TransitionRibbonPreviewChanged(previewGhost.PreviewEventData, changedNodes, changedAggregates))
                {
                    // Keep the queue count authoritative when an edit refreshes a pending child ribbon.
                    glsGroupAppearance.UpdateTransitionRibbon(previewGhost, previewBoostResolver);
                    previewGhost.ClearPreviewRibbonDirty();
                }
            }
        }

        private static bool TransitionRibbonPreviewChanged(
            BaseGLSEvent previewEvent,
            HashSet<BaseLightColorBase> changedNodes,
            Dictionary<BaseEventBoxGroup, HashSet<float>> changedAggregates) =>
            previewEvent is BaseLightColorBase colorEvent
                && (changedNodes.Contains(colorEvent)
                    || (colorEvent.EventBoxGroupData != null
                        && changedAggregates.TryGetValue(colorEvent.EventBoxGroupData, out var changedTimes)
                        && changedTimes.Contains(colorEvent.RelativeJsonTime)));

        private void ConfigureAsPreviewGhost(bool boost, Func<float, bool> isBoostAt, bool deferRibbonUpdate)
        {
            // Set both sides of the shader role before SetAppearance's existing upload so recycled primaries cannot retain ghost dithering.
            PreparePreviewOpacity();
            glsGroupAppearance.SetAppearance(this, true, boost);
            ApplyInnerPreviewShrink();
            // ScrollingShowsEveryGlsNodeBeforeRibbonWork makes the node visible now and queues only its color ribbon.
            if (deferRibbonUpdate && PreviewEventData is BaseLightColorBase)
            {
                lightGradientController.SetVisible(false);
                if (incomingLightGradientController != null)
                {
                    incomingLightGradientController.SetVisible(false);
                }
                MarkPreviewRibbonDirty();
            }
            else
            {
                glsGroupAppearance.UpdateTransitionRibbon(this, isBoostAt);
                ClearPreviewRibbonDirty();
            }
            // Give unmanaged previews the same selection outline color as their collection-owned group.
            SetOutlineColor(SelectionController.SelectedColor);
            UpdateGridPosition();
        }

        // ScrollingShowsEveryGlsNodeBeforeRibbonWork tracks pending work on the collection owner so a scrub can replace queued identities safely.
        private void MarkPreviewRibbonDirty()
        {
            if (previewRibbonDirty)
                return;
            previewRibbonDirty = true;
            var owner = previewOwner != null ? previewOwner : this;
            owner.pendingPreviewRibbonCount++;
        }

        private void ClearPreviewRibbonDirty()
        {
            if (!previewRibbonDirty)
                return;
            previewRibbonDirty = false;
            var owner = previewOwner != null ? previewOwner : this;
            owner.pendingPreviewRibbonCount--;
        }

        // A single ribbon is uploaded per work unit; the scheduler can spend all remaining frame time on cheap units.
        public bool ProcessNextPreviewRibbon()
        {
            if (pendingPreviewRibbonCount == 0)
                return true;

            for (var index = nextPreviewRibbonIndex; index <= previewGhosts.Count; index++)
            {
                var preview = index == 0 ? this : previewGhosts[index - 1];
                if (!preview.previewRibbonDirty)
                    continue;

                glsGroupAppearance.UpdateTransitionRibbon(preview, previewBoostResolver);
                preview.ClearPreviewRibbonDirty();
                nextPreviewRibbonIndex = index + 1;
                return pendingPreviewRibbonCount == 0;
            }

            nextPreviewRibbonIndex = 0;
            return pendingPreviewRibbonCount == 0;
        }

        public bool HasPendingPreviewRibbons => pendingPreviewRibbonCount > 0;

        private void ApplyInnerPreviewShrink()
        {
            if (!isPreviewGhost)
                return;

            var shrinkSetting = Settings.Instance.GLSInnerEventPreviewShrink;
            if (shrinkSetting != cachedInnerPreviewShrink)
            {
                cachedInnerPreviewShrink = shrinkSetting;
                cachedInnerPreviewVisualScale = Vector3.one * (1f - Mathf.Clamp01(shrinkSetting));
            }

            previewVisualRoot.localScale = cachedInnerPreviewVisualScale;
            previewVisualRoot.localPosition = new Vector3(
                0f,
                (cachedInnerPreviewVisualScale.y - 1f) / 2f,
                0f);
        }

        private void PreparePreviewOpacity()
        {
            // Always overwrite both values because the same cached renderer can alternate between primary and ghost roles.
            MpbController.Mpb.SetFloat(alwaysTranslucentId, isPreviewGhost ? 1f : 0f);
            MpbController.Mpb.SetFloat(
                translucentAlphaId,
                isPreviewGhost
                    ? Mathf.Clamp01(Settings.Instance.GLSOuterTrackGhostNodeOpacity)
                    : 1f);
        }

        private void ClearPreviewGhosts()
        {
            SetColorHover(false);
            // A recycled hovered ghost loses its owner reference, so clear the owner now to prevent stale primary highlights.
            Highlighted = false;

            foreach (var previewGhost in previewGhosts)
                ReleasePreviewGhost(previewGhost);

            previewGhosts.Clear();
            previewGhostByEvent.Clear();
            retainedPreviewGhosts.Clear();
            configuredPreviewGhosts.Clear();
            desiredPreviewEventSet.Clear();
            desiredPreviewEvents.Clear();
            reusePreviewCapacityOnNextConfigure = false;
        }

        public void SuspendPreviewGhosts()
        {
            previewConfigurationStage = PreviewConfigurationStage.None;
            SetColorHover(false);
            Highlighted = false;
            // ResetForPool may already have hidden this reusable root, so avoid repeating the Unity activation call in deferred suspension.
            if (previewGhostRoot != null && previewGhostRoot.gameObject.activeSelf)
            {
                previewGhostRoot.gameObject.SetActive(false);
            }
            reusePreviewCapacityOnNextConfigure = true;
        }

        public void BeginPreviewGhostSuspension() => previewConfigurationStage = PreviewConfigurationStage.SuspendRoot;

        public void CancelPreviewNodeConfiguration()
        {
            previewConfigurationStage = PreviewConfigurationStage.None;
            if (EventBoxGroupData == null)
                SuspendPreviewGhosts();
        }

        // GLSScrolling can preserve the costly ghost activation only within the current pool refresh.
        public void ResetForPool(bool keepActiveForRefresh = false)
        {
            // ResetForPoolClearsEveryExternallyVisibleOwnerState requires a pooled body to be inert and solid while retaining only its costly ghost capacity.
            previewConfigurationStage = PreviewConfigurationStage.None;
            // A recycled owner keeps its ghost slots but must discard every ribbon job tied to the old group.
            previewRibbonDirty = false;
            pendingPreviewRibbonCount = 0;
            nextPreviewRibbonIndex = 0;
            deferPreviewRibbons = false;
            foreach (var previewGhost in previewGhosts)
            {
                previewGhost.previewRibbonDirty = false;
            }
            previewConfigurationEventIndex = 0;
            previewConfigurationReleaseIndex = -1;
            previewConfigurationPreviousOffset = 0f;
            previewConfigurationForceAppearanceRefresh = false;
            previewConfigurationUsesGroupAppearance = false;
            previewBoostResolver = null;
            configuredPrimaryPreviewEvent = null;
            preservePreviewSlotsOnNextConfigure = false;
            previewSlotsConfigured = false;
            // GLSScrolling avoids tearing down a ghost root that will be rebound before this refresh returns.
            HasActivePooledPreviewRoot = keepActiveForRefresh;
            groupDragActive = false;
            groupWasSelectedBeforeDrag = false;
            isPreviewGhost = false;
            ResetInteractionState();
            GlsLightCount = 0;
            MpbController.Mpb.SetFloat(alwaysTranslucentId, 0f);
            MpbController.Mpb.SetFloat(translucentAlphaId, 1f);
            if (lightGradientController.gameObject.activeSelf)
            {
                lightGradientController.SetVisible(false);
            }
            if (incomingLightGradientController != null
                && incomingLightGradientController.gameObject.activeSelf)
            {
                incomingLightGradientController.SetVisible(false);
            }
            // Ordinary pooling hides separately-parented children now; same-refresh reuse delays only this expensive transition.
            if (!keepActiveForRefresh && previewGhostRoot != null && previewGhostRoot.gameObject.activeSelf)
            {
                previewGhostRoot.gameObject.SetActive(false);
            }
            EventBoxGroupData = null;
            PreviewEventData = null;
            previewOwner = null;
        }

        // GLSScrolling still deactivates ghosts whose owner had no same-refresh replacement.
        public void DeactivateUnusedPooledPreviewRoot()
        {
            HasActivePooledPreviewRoot = false;
            SuspendPreviewGhosts();
        }

        private void BindPreviewState(
            GLSGroupContainer owner,
            BaseEventBoxGroup group,
            BaseGLSEvent previewEvent,
            int lightCount,
            bool resetInteractionState)
        {
            if (resetInteractionState)
            {
                ResetInteractionState();
            }
            // RecycledPreviewNodesReceiveTheirRoleOnEveryBind makes child identity independent of whichever pool reset last touched it.
            isPreviewGhost = true;
            EventBoxGroupData = group;
            PreviewEventData = previewEvent;
            previewOwner = owner;
            GlsLightCount = lightCount;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        private void ResetInteractionState()
        {
            SetColorHover(false);
            Highlighted = false;
            Selected = false;
            Dragged = false;
        }

        private void ReleasePreviewGhost(GLSGroupContainer previewGhost)
        {
            // Released ghosts must not keep a stale ribbon job attached to their former owner.
            previewGhost.ClearPreviewRibbonDirty();
            // Disable before pooling so ghost renderers and hit-test colliders stop participating this frame.
            previewGhost.gameObject.SetActive(false);
            previewGhost.ResetForPool();
            if (previewGhostRoot != null)
                previewGhost.transform.SetParent(previewGhostRoot.parent, false);
            previewGhostPool.Push(previewGhost);
        }

        private GLSGroupContainer GetPreviewGhost()
        {
            GLSGroupContainer ghost;
            // Discard Unity-destroyed entries retained by the static pool across map reloads.
            while (previewGhostPool.TryPop(out ghost) && ghost == null) { }

            if (ghost == null)
            {
                ghost = Instantiate(this, transform.parent);
                ghost.isPreviewGhost = true;
                ghost.CreatePreviewVisualRoot();
            }

            ghost.transform.SetParent(GetPreviewGhostRoot(), false);
            // Instantiating a hovered owner and reusing a hovered ghost both copy transient interaction state.
            ghost.ResetInteractionState();
            // Restore the owner lane before UpdateGridPosition updates the event-specific Z coordinate.
            var position = transform.localPosition;
            ghost.transform.localPosition = new Vector3(position.x, position.y, ghost.transform.localPosition.z);
            ghost.gameObject.SetActive(true);
            return ghost;
        }

        private void CreatePreviewVisualRoot()
        {
            previewVisualRoot = new GameObject("GLS Preview Node Visuals").transform;
            previewVisualRoot.SetParent(transform, false);
            var outgoingRibbon = lightGradientController.transform;
            var incomingRibbon = incomingLightGradientController != null
                ? incomingLightGradientController.transform
                : null;
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index);
                if (child == previewVisualRoot || child == outgoingRibbon || child == incomingRibbon)
                    continue;
                child.SetParent(previewVisualRoot, false);
            }

            iconView.SetPreviewVisualParent(previewVisualRoot);
        }

        private Transform GetPreviewGhostRoot()
        {
            if (previewGhostRoot == null)
            {
                var root = new GameObject("GLS Preview Ghost Root");
                previewGhostRoot = root.transform;
                SetPreviewParent(transform.parent);
            }

            return previewGhostRoot;
        }

        public void SetPreviewParent(Transform parent)
        {
            if (previewGhostRoot == null)
                return;
            if (previewGhostRoot.parent == parent)
                return;
            previewGhostRoot.SetParent(parent, false);
            previewGhostRoot.localPosition = Vector3.zero;
            previewGhostRoot.localRotation = Quaternion.identity;
            previewGhostRoot.localScale = Vector3.one;
        }

        public void SetText(bool enable)
        {
            foreach (var textMeshPro in valueDisplays) textMeshPro.enabled = enable;
        }

        public void SetText(string text)
        {
            foreach (var textMeshPro in valueDisplays) textMeshPro.SetText(text);
        }

        public void SetIcons(BaseGLSEvent previewEvent)
        {
            iconView.SetIcons(GLSEventIconResolver.Resolve(previewEvent), previewEvent, valueDisplays);
        }

        public void SetColorHover(bool visible) => iconView.SetColorHover(visible, valueDisplays);

        // PR 666 renamed the lookup model; retain GLS icon behavior alongside the new API.
        public static float GetPositionFromTrackDefinition(TrackDefinitionsSO trackDefinitions, BaseEventBoxGroup data)
        {
            var track = trackDefinitions.GetGlsOrDefault(data.ID);

            var offset = 0f;
            if (track.ColorTrack)
            {
                if (data is BaseLightColorEventBoxGroup) return offset;
                offset++;
            }

            if (track.RotationTracks.Any(x => x))
            {
                if (data is BaseLightRotationEventBoxGroup) return offset;
                offset++;
            }

            if (track.TranslationTracks.Any(x => x))
            {
                if (data is BaseLightTranslationEventBoxGroup) return offset;
                offset++;
            }

            if (track.FloatFXTrack && data is BaseVfxEventEventBoxGroup) return offset;

            return -1f;
        }
    }
}
