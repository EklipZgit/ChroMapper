using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Info;
using Beatmap.V3;

public class ChromaReq : HeckRequirementCheck
{
    public override string Name => "Chroma";

    public override RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map)
    {
        if (infoDifficulty != null && (HasEnvironmentRemoval(infoDifficulty, map) || HasChromaEvents(map) || HasChromaTracks(map)))
            return RequiresChroma(infoDifficulty, map) ? RequirementType.Requirement : RequirementType.Suggestion;

        return RequirementType.None;
    }

    // GLS colour events use ChromaGLS. Keep them out of this check even if BaseDifficulty.IsChroma
    // later grows to include GLS nodes, so a GLS-only map does not advertise both mods.
    private bool HasChromaEvents(BaseDifficulty map) =>
        map.Notes.Any(x => x.IsChroma())
        || map.Arcs.Any(x => x.IsChroma())
        || map.Chains.Any(x => x.IsChroma())
        || map.Obstacles.Any(x => x.IsChroma())
        || map.Events.Any(x => x.IsChroma())
        || map.EnvironmentEnhancements.Any();

    private bool RequiresChroma(InfoDifficulty infoDifficulty, BaseDifficulty map) =>
        infoDifficulty.CustomRequirements.Any(x => x == "Chroma");

    private bool HasEnvironmentRemoval(InfoDifficulty infoDifficulty, BaseDifficulty map) =>
        (infoDifficulty.CustomData != null && infoDifficulty.CustomData.HasKey("_environmentRemoval") &&
         infoDifficulty.CustomData["_environmentRemoval"].AsArray.Count > 0) ||
        (map.CustomData != null &&
         map.CustomData.HasKey("_environment") &&
         map.CustomData["_environment"].AsArray.Count > 0);
    
    private static readonly List<string> chromaSpecificTrackTypes = new List<string> { "AnimateComponent" };

    private static readonly List<string> v3ChromaAnimationKeys = new List<string> { "color" };

    private static readonly List<string> v2ChromaAnimationKeys = new List<string> { "_color" };

    private bool HasChromaTracks(BaseDifficulty map)
    {
        var chromaAnimationKeys = Settings.Instance.MapVersion switch
        {
            3 => v3ChromaAnimationKeys,
            2 => v2ChromaAnimationKeys,
            _ => new List<string>(),
        };
        return HasAnimationsFromMod(map, chromaSpecificTrackTypes, chromaAnimationKeys);
    }
}
