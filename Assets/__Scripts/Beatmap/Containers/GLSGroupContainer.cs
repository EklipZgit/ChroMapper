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
        // PR 666 renamed the track-definition asset; the GLS previews must use that authoritative type.
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

        private Transform previewGhostRoot;

        // Retain the boost lookup so existing source nodes can refresh ribbons after a later target changes easing.
        private Func<float, bool> previewBoostResolver;

        // Distinguish the collection-owned node from its translucent, dynamically-created previews.
        private bool isPreviewGhost;

        private bool preservePreviewSlotsOnNextConfigure;

        private bool reusePreviewCapacityOnNextConfigure;

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
                    reusePreviewCapacityOnNextConfigure = true;

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
            pos.y = BeatmapConstant.EventNodeGroundedCenterY;
            // Unity preview events need explicit null checks before choosing the rendered beat position.
            var previewSongBpmTime = PreviewEventData != null
                ? PreviewEventData.SongBpmTime
                : EventBoxGroupData.SongBpmTime;
            pos.z = previewSongBpmTime * EditorScaleController.EditorScale;
            transform.localPosition = pos;
            UpdateCollisionGroups();

            // Pooled outer-track previews are not collection-owned, so forward global editor-scale position refreshes to them.
            if (!isPreviewGhost)
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
        }

        public void BeginPreviewNodeConfiguration(
            Func<float, bool> isBoostAt,
            float lowerBound,
            float upperBound,
            ISet<BaseGLSEvent> retainedEvents,
            bool forceAppearanceRefresh,
            bool hideUntilConfigured)
        {
            // Preserve the collection's boost resolver for targeted ribbon-only refreshes that do not rebuild hover objects.
            previewBoostResolver = isBoostAt;
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
                        if (previewConfigurationUsesGroupAppearance)
                        {
                            ConfigureAsPreviewGhost(
                                previewBoostResolver(EventBoxGroupData.JsonTime),
                                previewBoostResolver);
                            configuredPrimaryPreviewEvent = null;
                            return false;
                        }
                        if (PreviewEventData != null && previewConfigurationForceAppearanceRefresh)
                        {
                            ConfigureAsPreviewGhost(
                                previewBoostResolver(PreviewEventData.JsonTime),
                                previewBoostResolver);
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
                        previewBoostResolver);
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
            glsGroupAppearance.UpdateTransitionRibbon(this, previewBoostResolver);
            foreach (var previewGhost in previewGhosts)
                glsGroupAppearance.UpdateTransitionRibbon(previewGhost, previewBoostResolver);
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
                glsGroupAppearance.UpdateTransitionRibbon(this, previewBoostResolver);
            foreach (var previewGhost in previewGhosts)
            {
                if (TransitionRibbonPreviewChanged(previewGhost.PreviewEventData, changedNodes, changedAggregates))
                    glsGroupAppearance.UpdateTransitionRibbon(previewGhost, previewBoostResolver);
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

        private void ConfigureAsPreviewGhost(bool boost, Func<float, bool> isBoostAt)
        {
            glsGroupAppearance.SetAppearance(this, true, boost);
            // Rebuild this preview's cross-group color ribbon whenever its represented inner node changes.
            glsGroupAppearance.UpdateTransitionRibbon(this, isBoostAt);
            ApplyPreviewOpacity();
            // Give unmanaged previews the same selection outline color as their collection-owned group.
            SetOutlineColor(SelectionController.SelectedColor);
            UpdateGridPosition();
        }

        private void ApplyPreviewOpacity()
        {
            if (!isPreviewGhost) return;

            // Match passed notes by enabling the shader branch that consumes _TranslucentAlpha.
            var opacity = Mathf.Clamp01(Settings.Instance.GLSOuterTrackGhostNodeOpacity);
            MpbController.Mpb.SetFloat(alwaysTranslucentId, 1f);
            MpbController.Mpb.SetFloat(translucentAlphaId, opacity);
            MpbController.ApplyChanges();
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
            // Ghosts remain reusable and keep their prepared appearance while one root activation removes every
            // renderer and collider from the playback frame that recycled this owner.
            if (previewGhostRoot != null)
                previewGhostRoot.gameObject.SetActive(false);
            reusePreviewCapacityOnNextConfigure = true;
        }

        public void BeginPreviewGhostSuspension() => previewConfigurationStage = PreviewConfigurationStage.SuspendRoot;

        public void CancelPreviewNodeConfiguration()
        {
            previewConfigurationStage = PreviewConfigurationStage.None;
            if (EventBoxGroupData == null)
                SuspendPreviewGhosts();
        }

        public void ResetForPool()
        {
            previewConfigurationStage = PreviewConfigurationStage.None;
            previewBoostResolver = null;
            configuredPrimaryPreviewEvent = null;
            preservePreviewSlotsOnNextConfigure = false;
            groupDragActive = false;
            groupWasSelectedBeforeDrag = false;
            ResetInteractionState();
            EventBoxGroupData = null;
            PreviewEventData = null;
            previewOwner = null;
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
