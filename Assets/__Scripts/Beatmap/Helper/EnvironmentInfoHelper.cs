using Beatmap.Info;

public static class EnvironmentInfoHelper
{
    private const string TheSecondEnvironmentId = "TheSecondEnvironment";

    public static string GetCurrentEnvironment() =>
        GetCurrentEnvironment(BeatSaberSongContainer.Instance.Info, BeatSaberSongContainer.Instance.MapDifficultyInfo);

    public static string GetCurrentEnvironment(InfoDifficulty mapInfo) =>
        GetCurrentEnvironment(BeatSaberSongContainer.Instance.Info, mapInfo);

    public static string GetCurrentEnvironment(BaseInfo info, InfoDifficulty mapInfo) =>
        GetCurrentEnvironment(info, mapInfo, mapInfo.EnvironmentNameIndex);

    public static string GetCurrentEnvironment(BaseInfo info, InfoDifficulty mapInfo, int index)
    {
        if (index >= 0 && index < info.EnvironmentNames.Count) return info.EnvironmentNames[index];

        return mapInfo.Characteristic is "90Degree" or "360Degree"
            ? info.AllDirectionsEnvironmentName
            : info.EnvironmentName;
    }
}
