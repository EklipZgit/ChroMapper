using System.Linq;
using Beatmap.Base;
using Beatmap.Info;

/// <summary>
/// Detects custom RGB data on Group Lighting System colour events.
/// GLS colours are implemented by ChromaGLS rather than the regular Chroma plugin.
/// </summary>
public class ChromaGLSReq : RequirementCheck
{
    public override string Name => "ChromaGLS";

    public override RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map) =>
        HasChromaGLSEvents(map) ? RequirementType.Suggestion : RequirementType.None;

    private static bool HasChromaGLSEvents(BaseDifficulty map) =>
        map.LightColorEventBoxGroups
            .SelectMany(group => group.Boxes)
            .SelectMany(box => box.Events)
            .Any(lightEvent => lightEvent.IsChroma());
}
