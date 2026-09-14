using Beatmap.Base;
using Beatmap.Info;
// TrueHSVBasicEventsSuggestBeatToTheFutureOnSave shares the playback classifier so legacy and canonical spellings stay consistent.
using Beatmap.Shared;

// BeatToTheFuture owns the flat V3 VNJS runtime path, so preserved events require that capability.
public class BeatToTheFutureReq : RequirementCheck
{
    public override string Name => "BeatToTheFuture";

    public override RequirementType IsRequiredOrSuggested(InfoDifficulty infoDifficulty, BaseDifficulty map)
    {
        // Saved V3 VNJS and raw y=3/4 walls need the backport, so they remain stronger than the ring-speed suggestion.
        if (Settings.Instance.MapVersion == 3
            && ((map.SaveVNJSEventsInV3 && map.NJSEvents.Count > 0) || map.HasV4UpperWalls()))
        {
            return RequirementType.Requirement;
        }

        // TrueHSVBasicEventsSuggestBeatToTheFutureOnSave adds only a suggestion; TrueHSVSuggestionIsCoveredByExistingRequirements preserves an explicit stronger declaration.
        if (RingPropagationCompatibility.HasOldPropagationDeclaration(infoDifficulty.CustomData)
            || RingPropagationCompatibility.HasOldPropagationDeclaration(map.RuntimeLevelCustomData)
            || RingPropagationCompatibility.HasOldPropagationDeclaration(map.CustomData)
            || HasTrueHsvEvents(map))
        {
            return infoDifficulty.CustomRequirements.Contains(Name)
                ? RequirementType.Requirement
                : RequirementType.Suggestion;
        }

        return RequirementType.None;
    }

    // TrueHSVSuggestionUsesFinalSaveRequirements defers redundancy checks until every checker has updated the final saved requirements.
    public override bool IsSuggestionCoveredByRequirements(InfoDifficulty infoDifficulty) =>
        base.IsSuggestionCoveredByRequirements(infoDifficulty) || infoDifficulty.CustomRequirements.Contains("ChromaGLS");

    // TrueHSVSuggestionTracksEditsAndRemoval scans only authoritative Basic Events once per save, short-circuiting without visual discovery or per-frame work.
    private static bool HasTrueHsvEvents(BaseDifficulty map)
    {
        foreach (var evt in map.Events)
        {
            if (BasicEventColorLerp.FromSerializedName(evt.CustomLerpType) == BasicEventColorLerpType.TrueHSV)
                return true;
        }

        return false;
    }
}
