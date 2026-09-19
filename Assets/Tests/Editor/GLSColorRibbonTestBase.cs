using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
// Ribbon material evaluation uses the production RGB/HSV interpolation contract without refreshing its visuals.
using Beatmap.Shared;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    // GLS ribbon mutation regressions share one independent map evaluator so every input path proves both cached
    // live-light output and already-rendered ribbon state without repairing either cache from the assertion itself.
    public abstract class GLSColorRibbonTestBase : TestBase
    {
        private static readonly int colorAId = Shader.PropertyToID("_ColorA");
        private static readonly int colorBId = Shader.PropertyToID("_ColorB");
        private static readonly int easingId = Shader.PropertyToID("_EasingID");
        private static readonly int useHsvId = Shader.PropertyToID("_UseHSV");
        // Physical GLS ribbons render from an uploaded per-light timeline, so the oracle reads those existing
        // material properties rather than accidentally validating only the legacy unknown-light-count fallback.
        private static readonly int useLightTimelineId = Shader.PropertyToID("_UseLightTimeline");
        private static readonly int lightTimelineTextureId = Shader.PropertyToID("_LightDistributionTex");
        private static readonly int lightTimelineWidthId = Shader.PropertyToID("_LightDistributionWidth");

        private readonly List<GlsColorScenarioContext> colorPreviewContexts = new();

        // Register one synthetic physical group light before scenario placement so ordinary GLS manager callbacks own
        // every subsequent cache mutation exactly as they do for an environment light.
        protected GlsColorScenarioContext InitializeColorPreview(
            int groupId,
            int lightId = 0,
            int minimumLightCount = 0)
        {
            var runtimeContext = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            Assert.That(runtimeContext, Is.Not.Null, "The loaded test map had no runtime context.");
            var effectManager = runtimeContext.Descriptor.LightColorGroupEffectManager;
            // GLSColorRibbonMutationTest authors group zero even when the test environment has no physical group zero;
            // register one production effect so GLSManager mutations and the synthetic light share the normal cache path.
            var ownsEffect = !effectManager.IdToEffect.TryGetValue(groupId, out var effect);
            if (ownsEffect)
            {
                // GLSColorRibbonMutationTest authors filter lanes zero and one, so its synthetic effect must make both filters valid without warning spam.
                effect = effectManager.Register(groupId, Mathf.Max(lightId + 1, minimumLightCount));
                effect.Atsc = runtimeContext.Atsc;
                effect.ColorScheme = runtimeContext.ColorScheme;
                effect.ColorBoostEffect = effectManager.IdToEffect.Values
                    .Where(candidate => !ReferenceEquals(candidate, effect))
                    .Select(candidate => candidate.ColorBoostEffect)
                    .FirstOrDefault(candidate => candidate != null);
            }

            // Minimal test environments can omit color boost entirely, but LightColorGroupEffect subscribes and
            // unsubscribes unconditionally; give a synthetic effect a fully initialized lifecycle dependency.
            var ownsColorBoost = ownsEffect && effect.ColorBoostEffect == null;
            if (ownsColorBoost)
            {
                effect.ColorBoostEffect = effectManager.gameObject.AddComponent<ColorBoostEffect>();
                effect.ColorBoostEffect.Atsc = runtimeContext.Atsc;
                effect.ColorBoostEffect.ColorScheme = runtimeContext.ColorScheme;
                effect.ColorBoostEffect.Initialize();
            }

            Assert.That(lightId, Is.InRange(0, effect.Count - 1));
            var eventCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
            var glsAppearanceField = typeof(GLSEventGridContainer).GetField(
                "glsEventAppearance",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(glsAppearanceField, Is.Not.Null, "The GLS event collection appearance field was unavailable.");
            var glsAppearance = glsAppearanceField.GetValue(eventCollection) as GLSEventAppearanceSO;
            Assert.That(glsAppearance, Is.Not.Null, "The GLS event collection had no color appearance asset.");
            var eventAppearanceField = typeof(GLSEventAppearanceSO).GetField(
                "eventAppearance",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(eventAppearanceField, Is.Not.Null, "The GLS color appearance palette field was unavailable.");
            var eventAppearance = eventAppearanceField.GetValue(glsAppearance) as EventAppearanceSO;
            Assert.That(eventAppearance, Is.Not.Null, "The GLS color appearance had no event palette.");

            var lightObject = new GameObject($"GLS ribbon preview light {groupId}:{lightId}");
            var light = lightObject.AddComponent<GlsRibbonPreviewLightController>();
            light.Kind = LightController.LightKind.Group;
            light.Type = groupId;
            light.ID = lightId;
            effect.Register(light);

            // Synthetic groups are registered after environment load, so refresh the controller's effect roster once
            // during setup; later playhead movement then drives this effect through the normal live-preview callback.
            RefreshLightshowEffectRoster();

            // Initialize once before the edit under test; assertions intentionally never call these rebuild APIs.
            effect.Initialize();
            foreach (var group in BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups)
            {
                if (group.ID == groupId)
                {
                    effect.InsertData(group);
                }
            }

            effect.Refresh();
            var context = new GlsColorScenarioContext(
                groupId,
                lightId,
                effect,
                eventAppearance,
                light,
                lightObject,
                effectManager,
                ownsEffect,
                ownsColorBoost);
            colorPreviewContexts.Add(context);
            return context;
        }

        // Remove synthetic lights and restore the production effect's current authoritative-map state after each case
        // so parameterized mutation matrices cannot leak destroyed controllers or cached groups into the next case.
        [TearDown]
        public void DisposeColorPreviews()
        {
            foreach (var context in colorPreviewContexts)
            {
                context.Effect.Unregister(context.Light);
                Object.DestroyImmediate(context.LightObject);
                // Synthetic environment groups must not survive the parameterized case and redirect later GLSManager edits.
                if (context.OwnsEffect)
                {
                    context.EffectManager.IdToEffect.Remove(context.GroupId);
                    var entriesField = typeof(LightColorGroupEffectManager).GetField(
                        "effectEntries",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    Assert.That(entriesField, Is.Not.Null, "The GLS effect manager entry list was unavailable.");
                    var entries = entriesField.GetValue(context.EffectManager) as List<LightColorGroupEffectEntry>;
                    Assert.That(entries, Is.Not.Null, "The GLS effect manager entry list had an unexpected type.");
                    entries.RemoveAll(entry => ReferenceEquals(entry.Effect, context.Effect));
                    Object.DestroyImmediate(context.Effect);
                    if (context.OwnsColorBoost)
                    {
                        Object.DestroyImmediate(context.ColorBoostEffect);
                    }

                    // Removing a synthetic effect must also remove it from the lightshow's cached update roster.
                    RefreshLightshowEffectRoster();

                    continue;
                }

                context.Effect.Initialize();
                foreach (var group in BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups)
                {
                    if (group.ID == context.GroupId)
                    {
                        context.Effect.InsertData(group);
                    }
                }

                context.Effect.Refresh();
            }

            colorPreviewContexts.Clear();
        }

        // PopulateEffects is the production environment-load hook; invoking it only at fixture setup/teardown keeps
        // assertions read-only while making a late-registered synthetic group participate in ordinary time changes.
        private static void RefreshLightshowEffectRoster()
        {
            var lightshow = Object.FindAnyObjectByType<LightshowController>();
            Assert.That(lightshow, Is.Not.Null, "The loaded test map had no lightshow controller.");
            var populateEffects = typeof(LightshowController).GetMethod(
                "PopulateEffects",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(populateEffects, Is.Not.Null, "The lightshow effect-roster hook was unavailable.");
            populateEffects.Invoke(lightshow, null);
        }

        // Compare the live scene light and every already-rendered copy of its active source ribbon with values derived
        // directly from the current map. Moving the playhead exercises normal preview output but does not rebuild caches.
        protected static void AssertGlsRibbonAndLightPreviewsMatchExpected(
            GlsColorScenarioContext context,
            params float[] jsonTimes) =>
            AssertGlsRibbonAndLightPreviewsMatchExpected(context, null, jsonTimes);

        // Scenario labels keep a large parameter matrix diagnosable while preserving the same no-rebuild assertion path.
        protected static void AssertGlsRibbonAndLightPreviewsMatchExpected(
            GlsColorScenarioContext context,
            string scenario,
            params float[] jsonTimes)
        {
            Assert.That(jsonTimes, Is.Not.Empty, "At least one GLS preview sample time is required.");
            var timeline = BuildExpectedTimeline(context);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            if (timeline.Count == 0)
            {
                var expectedDefault = ResolveColor(context, new BaseLightColorBase());
                expectedDefault.a = 0f;
                foreach (var jsonTime in jsonTimes)
                {
                    atsc.MoveToJsonTime(jsonTime);
                    AssertColorsEqual(
                        expectedDefault,
                        context.Light.Color,
                        $"{scenario ?? "Empty GLS color scenario"}: default GLS light "
                        + $"{context.GroupId}:{context.LightId} at JSON beat {jsonTime}");
                }

                return;
            }

            // GLSColorRibbonMutationTest resolves the current rendered controller set once per checkpoint instead of rediscovering every scene container at every sampled beat.
            var renderedRibbons = CreateRenderedRibbonLookup();
            foreach (var jsonTime in jsonTimes)
            {
                var songBpmTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonTime);
                var state = EvaluateExpectedState(context, timeline, songBpmTime);
                atsc.MoveToJsonTime(jsonTime);

                AssertColorsEqual(
                    state.Color,
                    context.Light.Color,
                    $"{scenario ?? "GLS color scenario"}: live GLS light "
                    + $"{context.GroupId}:{context.LightId} at JSON beat {jsonTime}");
                AssertRenderedRibbonsMatchExpected(context, renderedRibbons, state, songBpmTime, jsonTime);
            }
        }

        // Derive checkpoints from every current interval after the mutation so deleting the first node never reuses a
        // hard-coded sample that now falls before the authoritative timeline's first event.
        protected static void AssertAllCurrentGlsIntervalsMatchExpected(
            GlsColorScenarioContext context,
            string scenario = null)
        {
            var map = BeatSaberSongContainer.Instance.Map;
            var timeline = BuildExpectedTimeline(context);
            if (timeline.Count == 0)
            {
                AssertGlsRibbonAndLightPreviewsMatchExpected(
                    context,
                    scenario,
                    Mathf.Max(0f, Object.FindAnyObjectByType<AudioTimeSyncController>().CurrentJsonTime));
                return;
            }

            var sampleJsonTimes = new List<float>();
            for (var eventIndex = 0; eventIndex < timeline.Count; eventIndex++)
            {
                var startSongBpmTime = timeline[eventIndex].SongBpmTime;
                if (eventIndex + 1 >= timeline.Count)
                {
                    // The final checkpoint remains strictly after the event boundary so seek updates cannot observe
                    // the preceding cached interval at an exact floating-point edge.
                    sampleJsonTimes.Add((float)map.SongBpmTimeToJsonTime(startSongBpmTime + 0.25f));
                    continue;
                }

                var duration = timeline[eventIndex + 1].SongBpmTime - startSongBpmTime;
                // GLSColorRibbonMutationTest already repeats this assertion for all 27 easing triples at every edit/undo checkpoint;
                // one non-symmetric interior sample distinguishes None/Linear/custom curves without multiplying scene seeks fivefold.
                sampleJsonTimes.Add((float)map.SongBpmTimeToJsonTime(startSongBpmTime + (duration * 0.37f)));
            }

            // Traverse the same authoritative checkpoints backward so a stale seek cursor cannot pass a forward-only comparison.
            var forwardSamples = sampleJsonTimes.Distinct().ToArray();
            var bidirectionalSamples = forwardSamples
                .Concat(forwardSamples.Reverse().Skip(1))
                .ToArray();
            AssertGlsRibbonAndLightPreviewsMatchExpected(context, scenario, bidirectionalSamples);
        }

        // Reproduce GLS group/filter selection and distribution from authoritative objects rather than consulting the
        // renderer's StateChunksContainer, transition index, linked-event cache, or visual-container dictionaries.
        private static List<ExpectedColorEvent> BuildExpectedTimeline(GlsColorScenarioContext context)
        {
            var map = BeatSaberSongContainer.Instance.Map;
            var groups = new List<ExpectedColorGroup>();
            foreach (var group in map.LightColorEventBoxGroups
                         .Where(candidate => candidate.ID == context.GroupId)
                         .OrderBy(candidate => candidate.JsonTime))
            {
                foreach (var box in group.Boxes)
                {
                    var filter = IndexFilterHelper.Convert(box.IndexFilter, context.Effect.Count);
                    if (filter == null)
                    {
                        continue;
                    }

                    var found = false;
                    var durationOrder = 0;
                    var distributionOrder = 0;
                    foreach (var (element, candidateDurationOrder, candidateDistributionOrder) in filter)
                    {
                        if (element != context.LightId)
                        {
                            continue;
                        }

                        found = true;
                        durationOrder = candidateDurationOrder;
                        distributionOrder = candidateDistributionOrder;
                        break;
                    }

                    if (!found || box.Events.Length == 0)
                    {
                        continue;
                    }

                    var beatStep = DistributionHelper.GetBeatStep(
                        DistributionHelper.GetDurationCount(filter),
                        (DistributionType)box.BeatDistributionType,
                        box.BeatDistribution,
                        box.Events[^1].RelativeJsonTime);
                    var brightnessStep = DistributionHelper.GetValueStep(
                        distributionOrder,
                        DistributionHelper.GetDistributionCount(filter),
                        (DistributionType)box.BrightnessDistributionType,
                        box.BrightnessDistribution,
                        (EaseType)box.Easing);
                    groups.Add(new ExpectedColorGroup(group, box, durationOrder, beatStep, brightnessStep));
                    // EventGroupEffect accepts only the first box in a group that targets one physical light.
                    break;
                }
            }

            var timeline = new List<ExpectedColorEvent>();
            for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                var group = groups[groupIndex];
                var maximumJsonTime = groupIndex + 1 < groups.Count
                    ? groups[groupIndex + 1].LocalJsonTime
                    : float.PositiveInfinity;
                for (var eventIndex = 0; eventIndex < group.Box.Events.Length; eventIndex++)
                {
                    var source = group.Box.Events[eventIndex];
                    var eventJsonTime = group.Group.JsonTime + source.RelativeJsonTime
                        + (group.DurationOrder * group.BeatStep);
                    if (eventJsonTime > maximumJsonTime)
                    {
                        continue;
                    }

                    var brightnessOffset = eventIndex == 0 && group.Box.BrightnessAffectFirst != 1
                        ? 0f
                        : group.BrightnessStep;
                    timeline.Add(new ExpectedColorEvent(
                        source,
                        eventJsonTime,
                        (float)map.JsonTimeToSongBpmTime(eventJsonTime),
                        source.Brightness + brightnessOffset));
                }
            }

            timeline.Sort((left, right) => left.SongBpmTime.CompareTo(right.SongBpmTime));
            return timeline;
        }

        // Evaluate the same authored transition contract as the live tween from plain timeline values, including
        // use-previous nodes and strobe fields, without reading or resetting the production effect's cached state.
        private static ExpectedColorState EvaluateExpectedState(
            GlsColorScenarioContext context,
            IReadOnlyList<ExpectedColorEvent> timeline,
            float songBpmTime)
        {
            var stateIndex = -1;
            for (var index = 0; index < timeline.Count; index++)
            {
                if (timeline[index].SongBpmTime > songBpmTime)
                {
                    break;
                }

                stateIndex = index;
            }

            Assert.That(stateIndex, Is.GreaterThanOrEqualTo(0), "GLS preview samples must not precede the first authored event.");
            var state = timeline[stateIndex];
            var start = ResolveUsePrevious(timeline, stateIndex);
            var nextIndex = stateIndex + 1;
            var end = nextIndex < timeline.Count && timeline[nextIndex].Source.UsePrevious == 0
                ? timeline[nextIndex]
                : start;
            var endTime = nextIndex < timeline.Count
                ? timeline[nextIndex].SongBpmTime
                : float.MaxValue;
            var easingEvent = ReferenceEquals(end, start)
                ? start.Source
                : end.Source;
            var instantDestination = !ReferenceEquals(end, start)
                && end.Source.Easing == (int)EaseType.None;
            var strobeEnd = instantDestination ? start : end;

            // Reproduce the authored tween contract independently: an instant destination holds the source strobe
            // track, zero-frequency endpoints collapse to their normal track, and optional curves remain per-track.
            var tween = new LightColorTween
            {
                StartTimeAlpha = state.SongBpmTime,
                StartTimeColor = state.SongBpmTime,
                StartColor = ResolveColor(context, start.Source),
                StartAlpha = start.Brightness,
                StartStrobeFrequency = GetStrobeFrequency(start.Source),
                StartStrobeBrightness = start.Source.StrobeBrightness,
                StartStrobeColor = start.Source.StrobeColor ?? ResolveColor(context, start.Source),
                EndTimeAlpha = endTime,
                EndTimeColor = endTime,
                EndColor = ResolveColor(context, end.Source),
                EndAlpha = end.Brightness,
                EndStrobeFrequency = GetStrobeFrequency(strobeEnd.Source),
                EndStrobeBrightness = strobeEnd.Source.StrobeBrightness,
                EndStrobeColor = strobeEnd.Source.StrobeColor ?? ResolveColor(context, strobeEnd.Source),
                StrobeFade = start.Source.StrobeFade == 1,
                Easing = Easing.FromID(easingEvent.Easing),
                ColorEasing = instantDestination || !easingEvent.ChromaColorEasing.HasValue
                    ? null
                    : Easing.FromID(easingEvent.ChromaColorEasing.Value),
                StrobeEasing = start.Source.ChromaStrobeEasing.HasValue
                    ? Easing.FromID(start.Source.ChromaStrobeEasing.Value)
                    : null,
                StrobeColorEasing = instantDestination || !end.Source.ChromaStrobeColorEasing.HasValue
                    ? null
                    : Easing.FromID(end.Source.ChromaStrobeColorEasing.Value),
                ColorLerpType = instantDestination
                    ? BasicEventColorLerpType.RGB
                    : end.Source.CustomLerpType
            };
            // A disabled strobe track contributes the normal endpoint rather than stale strobe-only values.
            if (tween.StartStrobeFrequency <= 0f)
            {
                tween.StartStrobeBrightness = tween.StartAlpha;
                tween.StartStrobeColor = tween.StartColor;
            }

            if (tween.EndStrobeFrequency <= 0f)
            {
                tween.EndStrobeBrightness = tween.EndAlpha;
                tween.EndStrobeColor = tween.EndColor;
            }

            // Frequencies are authored per JSON beat but LightColorTween advances on the song-time clock.
            var strobeScale = GLSEventCommon.GetStrobeFrequencyScale(
                BeatSaberSongContainer.Instance.Map,
                state.SongBpmTime);
            tween.StartStrobeFrequency *= strobeScale;
            tween.EndStrobeFrequency *= strobeScale;
            tween.UpdateTime(songBpmTime);
            var transitionTarget = nextIndex < timeline.Count
                ? timeline[nextIndex]
                : null;
            return new ExpectedColorState(tween.Color, state.Source, transitionTarget);
        }

        // Resolve chained extension nodes against the current authoritative sequence just as gameplay does.
        private static ExpectedColorEvent ResolveUsePrevious(IReadOnlyList<ExpectedColorEvent> timeline, int index)
        {
            while (index >= 0 && timeline[index].Source.UsePrevious == 1)
            {
                index--;
            }

            Assert.That(index, Is.GreaterThanOrEqualTo(0), "A GLS use-previous event had no authored predecessor.");
            return timeline[index];
        }

        // Resolve default palette colors from the initialized environment while custom colors remain wholly map-authored.
        private static Color ResolveColor(GlsColorScenarioContext context, BaseLightColorBase source) =>
            source.CustomColor
            ?? context.Effect.ColorScheme.GetColorFrom((LightColor)source.Color, false);

        // Match native cycles-per-beat and Chroma interval semantics without reaching into the live effect cache.
        // ZeroBrightnessStrobeKeepsFrequencyIntoTransition keeps the independent ribbon oracle on the production timing rule instead of suppressing black authored strobes.
        private static float GetStrobeFrequency(BaseLightColorBase source) =>
            GLSEventCommon.GetStrobeFrequency(source);

        // GLSColorRibbonMutationTest snapshots only existing controllers once per assertion checkpoint; it never refreshes or repairs their visuals.
        private static Dictionary<BaseLightColorBase, List<LightGradientController>> CreateRenderedRibbonLookup()
        {
            var result = new Dictionary<BaseLightColorBase, List<LightGradientController>>();
            var editingMode = Object.FindAnyObjectByType<EditModeContext>().EditingMode;
            if (editingMode == EditingMode.EventBox)
            {
                var innerCollection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
                foreach (var container in innerCollection.LoadedContainers.Values.OfType<GLSEventContainer>())
                {
                    // GLSColorRibbonMutationTest indexes only color-node containers from the heterogeneous inner GLS collection.
                    AddRenderedRibbon(result, container.EventData as BaseLightColorBase, container.LightGradientController);
                }
            }

            if (editingMode == EditingMode.GLS)
            {
                var outerCollection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
                var ownedParents = new HashSet<GLSGroupContainer>(
                    outerCollection.LoadedContainers.Values.OfType<GLSGroupContainer>());
                foreach (var container in Object.FindObjectsByType<GLSGroupContainer>(
                             FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    if (ownedParents.Contains(container.DragTarget))
                    {
                        // GLSColorRibbonMutationTest indexes only color previews from the heterogeneous outer GLS pool.
                        AddRenderedRibbon(result, container.PreviewEventData as BaseLightColorBase, container.lightGradientController);
                    }
                }
            }

            return result;
        }

        // Ribbon lookup construction preserves every rendered copy of one event so duplicate outer ghosts are all validated.
        private static void AddRenderedRibbon(
            Dictionary<BaseLightColorBase, List<LightGradientController>> lookup,
            BaseLightColorBase source,
            LightGradientController controller)
        {
            if (source == null || controller == null)
            {
                return;
            }

            if (!lookup.TryGetValue(source, out var controllers))
            {
                controllers = new List<LightGradientController>();
                lookup.Add(source, controllers);
            }

            controllers.Add(controller);
        }

        // Locate rendered nodes by their current authoritative child identity, then inspect only their existing ribbon
        // material and transform state so stale containers or stale shader data fail rather than being refreshed away.
        private static void AssertRenderedRibbonsMatchExpected(
            GlsColorScenarioContext context,
            IReadOnlyDictionary<BaseLightColorBase, List<LightGradientController>> renderedRibbons,
            ExpectedColorState state,
            float songBpmTime,
            float jsonTime)
        {
            renderedRibbons.TryGetValue(state.Source, out var controllers);

            Assert.That(
                controllers,
                Is.Not.Empty,
                $"No already-rendered inner or outer GLS node represented the active source at JSON beat {jsonTime}. "
                + GLSEventCommon.DescribeEvent(state.Source));
            foreach (var controller in controllers)
            {
                AssertRibbonController(context, controller, state, songBpmTime, jsonTime);
            }
        }

        // Evaluate the shader inputs at the current interval progress and require visibility, duration, endpoints, and
        // easing to agree with the authored GLS appearance contract, exposing stale geometry without refreshing it.
        private static void AssertRibbonController(
            GlsColorScenarioContext context,
            LightGradientController controller,
            ExpectedColorState state,
            float songBpmTime,
            float jsonTime)
        {
            Assert.That(controller, Is.Not.Null, "The rendered GLS node had no ribbon controller.");
            var renderer = controller.GetComponentInChildren<MeshRenderer>(true);
            Assert.That(renderer, Is.Not.Null, "The rendered GLS ribbon had no mesh renderer.");
            var source = state.Source;
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);

            // A known physical group uses the nine-row material timeline; evaluate that already-uploaded payload
            // directly so assertions cannot repair stale ribbon caches by asking GLSEventCommon for a new timeline.
            if (properties.GetFloat(useLightTimelineId) > 0.5f)
            {
                Assert.That(
                    controller.gameObject.activeSelf && renderer.enabled,
                    Is.True,
                    $"GLS ribbon visibility was stale at JSON beat {jsonTime} for {GLSEventCommon.DescribeEvent(source)}.");
                Assert.That(
                    controller.transform.localScale.x,
                    Is.EqualTo(controller.ColorTimelineDuration * EditorScaleController.EditorScale * (4f / 3f))
                        .Within(0.0001f),
                    $"GLS ribbon duration was stale for {GLSEventCommon.DescribeEvent(source)}.");
                var timelineActual = EvaluateRenderedTimelineColor(context, controller, properties, songBpmTime);
                // Include the owning outer light count so a recycled fallback node cannot masquerade as a current physical timeline.
                var outerOwner = controller.GetComponentInParent<GLSGroupContainer>(true);
                AssertColorsEqual(
                    state.Color,
                    timelineActual,
                    $"Rendered GLS ribbon at JSON beat {jsonTime} from {GLSEventCommon.DescribeEvent(source)} "
                    + $"(outer light count {(outerOwner != null ? outerOwner.GlsLightCount.ToString() : "n/a")})");
                return;
            }

            var transition = state.TransitionTarget?.Source;
            var shouldRender = transition is { UsePrevious: 0 }
                && transition.Easing != (int)EaseType.None
                && transition.JsonTime > source.JsonTime;
            Assert.That(
                controller.gameObject.activeSelf && renderer.enabled,
                Is.EqualTo(shouldRender),
                $"GLS ribbon visibility was stale at JSON beat {jsonTime} for {GLSEventCommon.DescribeEvent(source)}.");
            if (!shouldRender)
            {
                return;
            }

            var duration = transition.SongBpmTime - source.SongBpmTime;
            Assert.That(
                controller.transform.localScale.x,
                Is.EqualTo(duration * EditorScaleController.EditorScale * (4f / 3f)).Within(0.0001f),
                $"GLS ribbon duration was stale for {GLSEventCommon.DescribeEvent(source)}.");
            var progress = Mathf.InverseLerp(source.JsonTime, transition.JsonTime, jsonTime);
            var shaderEasing = Easing.ByName.Values.ElementAtOrDefault(properties.GetInt(easingId)) ?? Easing.Linear;
            var actual = BasicEventColorLerp.Interpolate(
                properties.GetColor(colorAId),
                properties.GetColor(colorBId),
                shaderEasing(progress),
                (BasicEventColorLerpType)properties.GetInt(useHsvId));
            var expected = BasicEventColorLerp.Interpolate(
                ResolveRibbonAppearanceColor(context, source),
                ResolveRibbonAppearanceColor(context, transition),
                Easing.FromID(transition.Easing)(progress),
                BasicEventColorLerpType.RGB);
            AssertColorsEqual(
                expected,
                actual,
                $"Rendered GLS ribbon at JSON beat {jsonTime} from {GLSEventCommon.DescribeEvent(source)}");
        }

        // Reproduce BasicGradient's nine-row per-light branch from its existing texture and scalar properties; this
        // reads rendered state only and deliberately does not consult the production GLS timeline or transition cache.
        private static Color EvaluateRenderedTimelineColor(
            GlsColorScenarioContext context,
            LightGradientController controller,
            MaterialPropertyBlock properties,
            float songBpmTime)
        {
            var width = Mathf.RoundToInt(properties.GetFloat(lightTimelineWidthId));
            Assert.That(width, Is.GreaterThan(context.LightId), "The GLS ribbon timeline omitted the asserted light.");
            var texture = properties.GetTexture(lightTimelineTextureId) as Texture2D;
            Assert.That(texture, Is.Not.Null, "The GLS ribbon timeline had no readable endpoint texture.");
            Assert.That(texture.height, Is.EqualTo(9), "The GLS ribbon timeline texture had an unexpected row count.");
            var x = width - context.LightId - 1;
            var times = texture.GetPixel(x, 4);
            var time = songBpmTime - controller.ColorTimelineStart;
            Assert.That(time, Is.GreaterThanOrEqualTo(times.r - 0.0001f), "The GLS ribbon interval started too late.");
            Assert.That(time, Is.LessThanOrEqualTo(times.g + 0.0001f), "The GLS ribbon interval ended too early.");

            var rates = texture.GetPixel(x, 5);
            var brightness = texture.GetPixel(x, 6);
            var flags = texture.GetPixel(x, 7);
            var easings = texture.GetPixel(x, 8);
            var alphaProgress = EvaluateShaderEasing(easings.r, Mathf.InverseLerp(times.r, times.g, time));
            var colorProgress = times.a == times.b
                ? 0f
                : EvaluateShaderEasing(easings.g, Mathf.InverseLerp(times.b, times.a, time));
            var normalFrom = texture.GetPixel(x, 0);
            var normalTo = texture.GetPixel(x, 1);
            var composedEndpoints = flags.a >= 2f;
            if (!composedEndpoints)
            {
                normalFrom.a = brightness.b;
                normalTo.a = brightness.a;
            }

            var color = BasicEventColorLerp.Interpolate(
                normalFrom,
                normalTo,
                colorProgress,
                (BasicEventColorLerpType)Mathf.RoundToInt(flags.b));
            if (!composedEndpoints)
            {
                color.a *= Mathf.LerpUnclamped(rates.b, rates.a, alphaProgress);
            }

            if (rates.r <= 0f && rates.g <= 0f)
            {
                return color;
            }

            var strobeFrom = texture.GetPixel(x, 2);
            var strobeTo = texture.GetPixel(x, 3);
            strobeFrom.a = flags.r;
            strobeTo.a = flags.g;
            var strobe = BasicEventColorLerp.Interpolate(
                strobeFrom,
                strobeTo,
                EvaluateShaderEasing(easings.b, Mathf.InverseLerp(times.b, times.a, time)),
                (BasicEventColorLerpType)Mathf.RoundToInt(flags.b));
            strobe.a = Mathf.LerpUnclamped(
                flags.r * brightness.r,
                flags.g * brightness.g,
                alphaProgress);
            var normalizedAlpha = Mathf.InverseLerp(times.r, times.g, time);
            var duration = times.g - times.r;
            var fadeEnabled = Mathf.Repeat(flags.a, 2f) >= 1f;
            var cycles = duration * ((rates.r * normalizedAlpha)
                + (0.5f * (rates.g - rates.r) * normalizedAlpha * normalizedAlpha));
            if (fadeEnabled && rates.r <= 0f && rates.g > 0f)
            {
                cycles -= (rates.r + rates.g) * duration * 0.5f;
            }

            var phase = Mathf.Repeat(cycles, 1f);
            var strobeMix = fadeEnabled
                ? EvaluateShaderEasing(easings.a, 1f - Mathf.Abs((phase * 2f) - 1f))
                : phase >= 0.5f ? 1f : 0f;
            return Color.LerpUnclamped(color, strobe, strobeMix);
        }

        // Shader IDs are stable positions in the shared easing registry, so reading the uploaded ID does not rebuild
        // either GLS cache and retains nonlinear/custom easing coverage in material comparisons.
        private static float EvaluateShaderEasing(float shaderId, float progress)
        {
            var easing = Easing.ByName.Values.ElementAtOrDefault(Mathf.RoundToInt(shaderId)) ?? Easing.Linear;
            return easing(Mathf.Clamp01(progress));
        }

        // Reproduce the authored GLS node/ribbon palette independently from GLSEventCommon so an appearance regression
        // cannot make stale shader data and the expected calculation fail in the same way.
        private static Color ResolveRibbonAppearanceColor(
            GlsColorScenarioContext context,
            BaseLightColorBase source)
        {
            const float dimmedColorFraction = 0.175f;
            var palette = context.EventAppearance;
            var rendererColor = source.CustomColor
                ?? (source.Color == (int)LightColor.Red
                    ? palette.RedColor
                    : source.Color == (int)LightColor.Blue
                        ? palette.BlueColor
                        : palette.WhiteColor);
            rendererColor.a *= source.Brightness;
            var maximumChannel = Mathf.Max(rendererColor.r, Mathf.Max(rendererColor.g, rendererColor.b));
            var hdrIntensity = Mathf.Max(maximumChannel, 1f);
            if (maximumChannel > 1f)
            {
                rendererColor.r /= maximumChannel;
                rendererColor.g /= maximumChannel;
                rendererColor.b /= maximumChannel;
            }

            // GLS hover brightness changes are represented by renderer alpha (including authored custom alpha),
            // then converted to the opaque node/ribbon palette against its dimmed off-color endpoint.
            var effectiveBrightness = rendererColor.a * hdrIntensity;
            rendererColor.a = 1f;
            var clampedOffColor = Color.Lerp(palette.OffColor, rendererColor, dimmedColorFraction);
            return Color.Lerp(clampedOffColor, rendererColor, effectiveBrightness);
        }

        // Report channel-level discrepancies so a mutation failure identifies whether color, authored alpha, or light
        // level remained stale after the action.
        private static void AssertColorsEqual(Color expected, Color actual, string operation)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f), $"{operation}: red differed.");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f), $"{operation}: green differed.");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f), $"{operation}: blue differed.");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f), $"{operation}: alpha differed.");
        }

        // Keep the registered light and effect identity stable while action replacements freely replace every map event.
        protected sealed class GlsColorScenarioContext
        {
            // The containing fixture constructs and inspects this opaque context while derived fixtures only pass it back.
            internal GlsColorScenarioContext(
                int groupId,
                int lightId,
                LightColorGroupEffect effect,
                EventAppearanceSO eventAppearance,
                GlsRibbonPreviewLightController light,
                GameObject lightObject,
                LightColorGroupEffectManager effectManager,
                bool ownsEffect,
                bool ownsColorBoost)
            {
                GroupId = groupId;
                LightId = lightId;
                Effect = effect;
                EventAppearance = eventAppearance;
                Light = light;
                LightObject = lightObject;
                EffectManager = effectManager;
                OwnsEffect = ownsEffect;
                OwnsColorBoost = ownsColorBoost;
                ColorBoostEffect = effect.ColorBoostEffect;
            }

            public int GroupId { get; }
            public int LightId { get; }
            // Shared fixture helpers need these stable dependencies, but mutation tests do not expose or replace them.
            internal LightColorGroupEffect Effect { get; }
            internal EventAppearanceSO EventAppearance { get; }
            internal GlsRibbonPreviewLightController Light { get; }
            internal GameObject LightObject { get; }
            internal LightColorGroupEffectManager EffectManager { get; }
            internal bool OwnsEffect { get; }
            internal bool OwnsColorBoost { get; }
            internal ColorBoostEffect ColorBoostEffect { get; }
        }

        // A test-owned controller captures the exact color sent by the production GLS group effect.
        internal sealed class GlsRibbonPreviewLightController : LightController
        {
            protected override bool Initialize() => true;

            public override void SetColor(Color color) => Color = color;
        }

        // Retain the chosen group box and distribution coordinates before building the plain expected event timeline.
        private sealed class ExpectedColorGroup
        {
            public ExpectedColorGroup(
                BaseLightColorEventBoxGroup group,
                BaseLightColorEventBox box,
                int durationOrder,
                float beatStep,
                float brightnessStep)
            {
                Group = group;
                Box = box;
                DurationOrder = durationOrder;
                BeatStep = beatStep;
                BrightnessStep = brightnessStep;
            }

            public BaseLightColorEventBoxGroup Group { get; }
            public BaseLightColorEventBox Box { get; }
            public int DurationOrder { get; }
            public float BeatStep { get; }
            public float BrightnessStep { get; }
            public float LocalJsonTime => Group.JsonTime + (DurationOrder * BeatStep);
        }

        // Store only authoritative event data plus calculated distribution output, never renderer cache state.
        private sealed class ExpectedColorEvent
        {
            public ExpectedColorEvent(BaseLightColorBase source, float jsonTime, float songBpmTime, float brightness)
            {
                Source = source;
                JsonTime = jsonTime;
                SongBpmTime = songBpmTime;
                Brightness = brightness;
            }

            public BaseLightColorBase Source { get; }
            public float JsonTime { get; }
            public float SongBpmTime { get; }
            public float Brightness { get; }
        }

        // Pair expected live output with the exact source and following authored node whose ribbon should be rendered.
        private sealed class ExpectedColorState
        {
            public ExpectedColorState(Color color, BaseLightColorBase source, ExpectedColorEvent transitionTarget)
            {
                Color = color;
                Source = source;
                TransitionTarget = transitionTarget;
            }

            public Color Color { get; }
            public BaseLightColorBase Source { get; }
            public ExpectedColorEvent TransitionTarget { get; }
        }
    }
}
