using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MeasureLinesController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI measureLinePrefab;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private RectTransform parent;
    [SerializeField] private GridChild gridChild;

    // Pre-computed songBpmTime for each json beat index (0, 1, 2, ..., totalJsonBeats).
    // Sorted in ascending order of songBpmTime.
    private float[] beatSongBpmTimes = Array.Empty<float>();
    private int totalJsonBeats;

    // Object pool: reusable TextMeshProUGUI instances
    private readonly List<TextMeshProUGUI> pool = new();

    // Maps currently visible json beat -> pool index
    private readonly Dictionary<int, int> visibleBeatToPoolIndex = new();

    // Tracks which pool objects are free
    private readonly Stack<int> freePoolIndices = new();

    // The visual beat origin in json time, cached from last RefreshMeasureLines
    private float cachedVisualBeatOriginJson;

    private bool init;

    private void Start()
    {
        // Ensure the prefab itself is part of the pool
        measureLinePrefab.gameObject.SetActive(false);
        pool.Add(measureLinePrefab);
        freePoolIndices.Push(0);

        atsc.OnTimeChanged += OnTimeChanged;
        EditorScaleController.OnEditorScaleChanged += OnEditorScaleChanged;
        BPMChangeGridContainer.OnBPMChangeRefreshed += RefreshMeasureLines;
        atsc.OnVisualBeatOriginChanged += OnVisualBeatOriginChanged;
    }

    private void OnDestroy()
    {
        atsc.OnTimeChanged -= OnTimeChanged;
        EditorScaleController.OnEditorScaleChanged -= OnEditorScaleChanged;
        BPMChangeGridContainer.OnBPMChangeRefreshed -= RefreshMeasureLines;
        atsc.OnVisualBeatOriginChanged -= OnVisualBeatOriginChanged;
    }

    private void OnTimeChanged()
    {
        if (UIMode.PreviewMode || !init) return;
        RefreshVisibility();
    }

    private void OnEditorScaleChanged(float obj)
    {
        if (!init) return;
        UpdateVisiblePositions();
    }

    private void OnVisualBeatOriginChanged(float obj) => RefreshMeasureLines();

    public void RefreshMeasureLines()
    {
        // Measure-line refreshes are routine and should not emit per-operation noise.
        init = false;

        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer.Map;

        cachedVisualBeatOriginJson = atsc.VisualBeatOriginJsonTime;

        var rawBeatsInSong = Mathf.FloorToInt(atsc.GetBeatFromSeconds(songContainer.LoadedSong.length));
        var modifiedBeatsInSong = Mathf.FloorToInt((float)map.SongBpmTimeToJsonTime(rawBeatsInSong));

        // Cap to prevent insanely high bpm events from creating too many beats
        modifiedBeatsInSong = Mathf.Min(rawBeatsInSong * 10, modifiedBeatsInSong);

        totalJsonBeats = modifiedBeatsInSong;

        // Pre-compute songBpmTime for every json beat
        if (beatSongBpmTimes.Length < totalJsonBeats + 1)
            beatSongBpmTimes = new float[totalJsonBeats + 1];

        for (var i = 0; i <= totalJsonBeats; i++)
            beatSongBpmTimes[i] = (float)map.JsonTimeToSongBpmTime(cachedVisualBeatOriginJson + i);

        // Set proper spacing between Notes grid, Measure lines, and Events grid
        gridChild.Lane = totalJsonBeats > 1000 ? 1 : 0;

        // Hide all currently visible objects and return them to the pool
        foreach (var kvp in visibleBeatToPoolIndex)
            pool[kvp.Value].gameObject.SetActive(false);

        visibleBeatToPoolIndex.Clear();
        freePoolIndices.Clear();
        for (var i = 0; i < pool.Count; i++)
        {
            pool[i].gameObject.SetActive(false);
            freePoolIndices.Push(i);
        }

        init = true;
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        var currentSongBpmBeat = atsc.CurrentSongBpmTime;
        var songBpmBeatsAhead = Settings.Instance.TrackLength;
        var songBpmBeatsBehind = songBpmBeatsAhead / 4f;

        var viewMin = currentSongBpmBeat - songBpmBeatsBehind;
        var viewMax = currentSongBpmBeat + songBpmBeatsAhead;

        // Binary search for the first json beat index in view
        var firstVisible = LowerBound(beatSongBpmTimes, totalJsonBeats + 1, viewMin);
        // Binary search for the last json beat index in view
        var lastVisible = UpperBound(beatSongBpmTimes, totalJsonBeats + 1, viewMax) - 1;

        firstVisible = Mathf.Max(0, firstVisible);
        lastVisible = Mathf.Min(totalJsonBeats, lastVisible);

        var editorScale = EditorScaleController.EditorScale;

        // Remove beats that scrolled out of view
        // Collect keys to remove to avoid modifying dictionary during iteration
        // Use a small stackalloc-style approach with a reusable list
        removeBuffer.Clear();
        foreach (var kvp in visibleBeatToPoolIndex)
        {
            if (kvp.Key < firstVisible || kvp.Key > lastVisible)
                removeBuffer.Add(kvp.Key);
        }

        for (var i = 0; i < removeBuffer.Count; i++)
        {
            var beat = removeBuffer[i];
            var poolIdx = visibleBeatToPoolIndex[beat];
            pool[poolIdx].gameObject.SetActive(false);
            freePoolIndices.Push(poolIdx);
            visibleBeatToPoolIndex.Remove(beat);
        }

        // Add beats that scrolled into view
        for (var jsonBeat = firstVisible; jsonBeat <= lastVisible; jsonBeat++)
        {
            if (visibleBeatToPoolIndex.ContainsKey(jsonBeat))
                continue;

            var poolIdx = AcquirePoolObject();
            visibleBeatToPoolIndex[jsonBeat] = poolIdx;

            var tmp = pool[poolIdx];
            tmp.text = jsonBeat.ToString();
            tmp.transform.localPosition = new Vector3(0, beatSongBpmTimes[jsonBeat] * editorScale, 0);
            tmp.gameObject.SetActive(true);
        }
    }

    // Reusable buffer to avoid per-frame allocations in RefreshVisibility
    private readonly List<int> removeBuffer = new();

    private void UpdateVisiblePositions()
    {
        var editorScale = EditorScaleController.EditorScale;
        foreach (var kvp in visibleBeatToPoolIndex)
            pool[kvp.Value].transform.localPosition = new Vector3(0, beatSongBpmTimes[kvp.Key] * editorScale, 0);
    }

    private int AcquirePoolObject()
    {
        if (freePoolIndices.Count > 0)
            return freePoolIndices.Pop();

        var newText = Instantiate(measureLinePrefab, parent);
        newText.gameObject.SetActive(false);
        var idx = pool.Count;
        pool.Add(newText);
        return idx;
    }

    /// <summary>
    /// Returns the index of the first element >= value.
    /// </summary>
    private static int LowerBound(float[] array, int length, float value)
    {
        int lo = 0, hi = length;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            if (array[mid] < value)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }

    /// <summary>
    /// Returns the index of the first element > value.
    /// </summary>
    private static int UpperBound(float[] array, int length, float value)
    {
        int lo = 0, hi = length;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            if (array[mid] <= value)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }
}
