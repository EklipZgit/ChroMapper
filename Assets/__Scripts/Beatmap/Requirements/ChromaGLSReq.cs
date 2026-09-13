// ExtendedGLSNodeEasingsRequireChromaGLSWhenSerialized scans typed read-only GLS data and reuses
// the V3 serializer's custom-color easing boundary instead of maintaining a second validity table.
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Info;
using Beatmap.V3;
using ZLinq;

/// <summary>
/// Requires ChromaGLS for extended GLS easings and suggests it for custom GLS colours/ring zoom.
/// These features are implemented by ChromaGLS rather than the regular Chroma plugin.
/// </summary>
public class ChromaGLSReq : RequirementCheck
{
    public override string Name => "ChromaGLS";

    // ExtendedGLSEasingRequirementOverridesSuggestions gives actual easing use precedence over cosmetic
    // suggestions; ExtendedGLSEasingRequirementTracksEditsAndRemoval requires a fresh result at each save.
    public override RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map)
    {
        if (HasRequiredGLSEasings(map))
            return RequirementType.Requirement;

        return HasChromaGLSEvents(map) || HasSmoothStepRingZoomOverride(map)
            ? RequirementType.Suggestion
            : RequirementType.None;
    }

    // UnreferencedExtendedEasingDoesNotRequireChromaGLS restricts this save-time scan to the four GLS
    // group categories, not unused FloatFX pools, NJS events, placement defaults, or loaded visual caches.
    private static bool HasRequiredGLSEasings(BaseDifficulty map)
    {
        var version = Settings.Instance.MapVersion;
        // V2OmittedGLSEasingsDoNotRequireChromaGLS excludes GLS data that the selected format does not save.
        if (version != 3 && version != 4)
            return false;

        return HasRequiredGLSEasings(map.LightColorEventBoxGroups, version == 3)
            || HasRequiredGLSEasings(map.LightRotationEventBoxGroups, false)
            || HasRequiredGLSEasings(map.LightTranslationEventBoxGroups, false)
            || HasRequiredGLSEasings(map.VfxEventBoxGroups, false);
    }

    // ExtendedGLSNodeEasingsRequireChromaGLSWhenSerialized uses authoritative box/event arrays directly:
    // the save scan is O(GLS boxes + nodes), allocation-free, short-circuiting, and independent of ordering.
    private static bool HasRequiredGLSEasings(IReadOnlyList<BaseEventBoxGroup> groups, bool usesV3ColorExtension)
    {
        for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            var boxes = groups[groupIndex].ReadOnlyBoxes;
            for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
            {
                var box = boxes[boxIndex];
                // ExtendedGLSDistributionEasingsRequireChromaGLS covers all four box categories: the game
                // calls the same unsupported converter for distribution easing even with native child curves.
                if (IsExtendedGameEasing(box.Easing))
                    return true;

                var events = box.ReadOnlyEvents;
                for (var eventIndex = 0; eventIndex < events.Count; eventIndex++)
                {
                    var easing = events[eventIndex] switch
                    {
                        BaseLightColorBase color => color.Easing,
                        BaseLightRotationBase rotation => rotation.EaseType,
                        BaseLightTranslationBase translation => translation.EaseType,
                        BaseFxEventFloat fx => fx.Easing,
                        _ => (int)EaseType.Linear
                    };

                    // VanillaGLSEasingRequirementsRespectV3ColorSchema shares the exact serializer predicate;
                    // V3ColorUsePreviousCustomEasingRequiresChromaGLS also retains Extend's authored curve.
                    if (usesV3ColorExtension
                        ? V3LightColorBase.RequiresCustomEasing(easing)
                        : IsExtendedGameEasing(easing))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // ExtendedGLSDistributionEasingsRequireChromaGLS covers the converter's contiguous missing families:
    // Sine, Cubic, Quartic, Quintic and Exponential, each with In/Out/InOut, and no unknown IDs.
    private static bool IsExtendedGameEasing(int easing) =>
        easing >= (int)EaseType.InSinusoidal && easing <= (int)EaseType.InOutExponential;

    private static bool HasChromaGLSEvents(BaseDifficulty map) =>
        map.LightColorEventBoxGroups
            .AsValueEnumerable()
            .SelectMany(group => group.Boxes)
            .SelectMany(box => box.Events)
            .Any(lightEvent => lightEvent.IsChroma());

    private static bool HasSmoothStepRingZoomOverride(BaseDifficulty map)
    {
        if (map.RuntimeTrackDefinitions == null)
            return false;

        // SmoothStepRingZoom only applies to The Second's legacy ring right now.
        return map.Events.AsValueEnumerable().Any(
            basicEvent => basicEvent.CustomStep.HasValue
                && map.RuntimeTrackDefinitions.GetBasicOrDefault(basicEvent.Type).Components
                    .HasFlag(BasicEventComponent.SmoothStepRingZoom));
    }
}
