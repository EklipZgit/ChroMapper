using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class MeasureLinesController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI measureLinePrefab;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private RectTransform parent;
    [SerializeField] private Transform measureLineGrid;
    [SerializeField] private BPMChangeGridContainer bpmChangeGridContainer;
    [SerializeField] private GridChild gridChild;
    [SerializeField] private BookmarkRenderingController bookmarkRenderingController;

    private readonly List<(float time, TextMeshProUGUI tmp)> measureTextsByBeat = new();
    private readonly Dictionary<float, TextMeshProUGUI> activeMeasureTexts = new();

    private bool init;

    private void Start()
    {
        if (!measureTextsByBeat.Any()) measureTextsByBeat.Add((0, measureLinePrefab));

        atsc.OnTimeChanged += UpdateTime;
        EditorScaleController.OnEditorScaleChanged += EditorScaleUpdated;
        BPMChangeGridContainer.OnBPMChangeRefreshed += RefreshMeasureLines;
    }

    private void OnDestroy()
    {
        atsc.OnTimeChanged -= UpdateTime;
        EditorScaleController.OnEditorScaleChanged -= EditorScaleUpdated;
        BPMChangeGridContainer.OnBPMChangeRefreshed -= RefreshMeasureLines;
    }

    private void UpdateTime()
    {
        if (UIMode.PreviewMode || !init) return;
        RefreshVisibility();
    }

    private void EditorScaleUpdated(float obj) => RefreshPositions();

    public void RefreshMeasureLines()
    {
        Debug.Log("Refreshing measure lines...");
        init = false;
        var existing = new Queue<TextMeshProUGUI>(measureTextsByBeat.Select(x => x.tmp));
        measureTextsByBeat.Clear();
        activeMeasureTexts.Clear();

        var songContainer = BeatSaberSongContainer.Instance;

        var rawBeatsInSong =
            Mathf.FloorToInt(atsc.GetBeatFromSeconds(songContainer.LoadedSong.length));
        var modifiedBeatsInSong =
            Mathf.FloorToInt((float)songContainer.Map.SongBpmTimeToJsonTime(rawBeatsInSong));

        // This stops CM freezing for a few seconds as a result of instantiating a bajillion lines from insanely
        // high bpm events. Should be reasonable to assume that you're not mapping at >10x the info bpm
        modifiedBeatsInSong = Mathf.Min(rawBeatsInSong * 10, modifiedBeatsInSong);

        var jsonBeat = 0;
        while (jsonBeat <= modifiedBeatsInSong)
        {
            var text = existing.Count > 0 ? existing.Dequeue() : Instantiate(measureLinePrefab, parent);
            text.gameObject.SetActive(false);
            text.text = $"{jsonBeat}";
            var jsonBeatPosition = (float)songContainer.Map.JsonTimeToSongBpmTime(jsonBeat);
            text.transform.localPosition = new Vector3(0, jsonBeatPosition * EditorScaleController.EditorScale, 0);
            measureTextsByBeat.Add((jsonBeatPosition, text));
            jsonBeat++;
        }

        // Set proper spacing between Notes grid, Measure lines, and Events grid
        gridChild.Lane = jsonBeat > 1000 ? 1 : 0;
        foreach (var leftovers in existing) Destroy(leftovers.gameObject);
        init = true;
        RefreshVisibility();
        RefreshPositions();
    }

    private void RefreshVisibility()
    {
        var currentSongBpmBeat = atsc.CurrentSongBpmTime;
        var songBpmBeatsAhead = Settings.Instance.TrackLength;
        var songBpmBeatsBehind = songBpmBeatsAhead / 4f;

        foreach (var (time, tmp) in activeMeasureTexts.ToArray())
        {
            if (currentSongBpmBeat - songBpmBeatsBehind <= time && time <= currentSongBpmBeat + songBpmBeatsAhead)
                continue;

            tmp.gameObject.SetActive(false);
            activeMeasureTexts.Remove(time);
        }

        var songContainer = BeatSaberSongContainer.Instance;
        foreach (var (time, tmp) in measureTextsByBeat.Skip(
            Mathf.CeilToInt((float)songContainer.Map.SongBpmTimeToJsonTime(currentSongBpmBeat - songBpmBeatsBehind))))
        {
            if (time > currentSongBpmBeat + songBpmBeatsAhead) break;
            if (activeMeasureTexts.ContainsKey(time)) continue;

            tmp.gameObject.SetActive(true);
            activeMeasureTexts[time] = tmp;
        }
    }

    private void RefreshPositions()
    {
        Debug.Log("Refreshing positions...");
        foreach (var kvp in measureTextsByBeat)
            kvp.tmp.transform.localPosition = new Vector3(0, kvp.time * EditorScaleController.EditorScale, 0);
    }
}
