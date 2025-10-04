using System.Collections.Generic;
using System.Linq;
using Beatmap.Base.Customs;
using TMPro;
using UnityEngine;


public class BookmarkRenderingController : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Transform frontNoteGridScaling;
    [SerializeField] private BookmarkManager manager;
    [SerializeField] private Transform gridBookmarksParent;

    private readonly List<CachedBookmark> renderedBookmarks = new();
    private readonly HashSet<CachedBookmark> activeBookmarks = new();

    private class CachedBookmark
    {
        public readonly BaseBookmark MapBookmark;
        public readonly TextMeshProUGUI Text;
        public string Name;
        public Color Color;

        public CachedBookmark(BaseBookmark bookmark, TextMeshProUGUI text)
        {
            MapBookmark = bookmark;
            Text = text;
            Name = bookmark.Name;
            Color = bookmark.Color;
        }
    }

    private void Start()
    {
        atsc.TimeChanged += UpdateTime;
        manager.BookmarksUpdated += UpdateRenderedBookmarks;
        EditorScaleController.EditorScaleChangedEvent += HandleEditorScaleChanged;
        BPMChangeGridContainer.OnBPMChangeRefreshed += HandleBPMChangeRefreshed;
        Settings.NotifyBySettingName(nameof(Settings.DisplayGridBookmarks), DisplayRenderedBookmarks);
        Settings.NotifyBySettingName(nameof(Settings.GridBookmarksHasLine), RefreshBookmarkGridLine);
    }

    private void OnDestroy()
    {
        atsc.TimeChanged -= UpdateTime;
        manager.BookmarksUpdated -= UpdateRenderedBookmarks;
        EditorScaleController.EditorScaleChangedEvent -= HandleEditorScaleChanged;
        BPMChangeGridContainer.OnBPMChangeRefreshed -= HandleBPMChangeRefreshed;
        Settings.ClearSettingNotifications(nameof(Settings.DisplayGridBookmarks));
        Settings.ClearSettingNotifications(nameof(Settings.GridBookmarksHasLine));
    }

    private void UpdateTime()
    {
        if (UIMode.PreviewMode) return;
        RefreshVisibility();
    }

    public void ClearCachedBookmarks()
    {
        activeBookmarks.Clear();
        for (var i = renderedBookmarks.Count - 1; i >= 0; i--)
        {
            var bookmark = renderedBookmarks[i];
            Destroy(bookmark.Text.gameObject);
            renderedBookmarks.Remove(bookmark);
        }
    }

    private void DisplayRenderedBookmarks(object _) => UpdateRenderedBookmarks();

    private void UpdateRenderedBookmarks()
    {
        var currentBookmarks = BeatSaberSongContainer.Instance.Map.Bookmarks;
        if (currentBookmarks.Count < renderedBookmarks.Count) // Removed bookmark
        {
            for (var i = renderedBookmarks.Count - 1; i >= 0; i--)
            {
                var bookmark = renderedBookmarks[i];
                if (!currentBookmarks.Contains(bookmark.MapBookmark))
                {
                    Destroy(bookmark.Text.gameObject);
                    renderedBookmarks.Remove(bookmark);
                    activeBookmarks.Remove(bookmark);
                    return;
                }
            }
        }
        else if (currentBookmarks.Count > renderedBookmarks.Count) // Added bookmark
        {
            foreach (var bookmark in currentBookmarks)
            {
                if (renderedBookmarks.All(x => x.MapBookmark != bookmark))
                {
                    var text = CreateGridBookmark(bookmark);
                    renderedBookmarks.Add(new CachedBookmark(bookmark, text));
                }
            }
        }
        else // Edited bookmark
        {
            foreach (var cachedBookmark in renderedBookmarks)
            {
                var mapBookmarkName = cachedBookmark.MapBookmark.Name;
                var mapBookmarkColor = cachedBookmark.MapBookmark.Color;

                if (cachedBookmark.Name != mapBookmarkName || cachedBookmark.Color != mapBookmarkColor)
                {
                    SetGridBookmarkNameColor(cachedBookmark.Text, mapBookmarkColor, mapBookmarkName);

                    cachedBookmark.Name = mapBookmarkName;
                    cachedBookmark.Color = mapBookmarkColor;
                }
            }
        }

        renderedBookmarks.Sort((a, b) => a.MapBookmark.SongBpmTime.CompareTo(b.MapBookmark.SongBpmTime));

        RefreshVisibility();
    }

    private void HandleEditorScaleChanged(float _)
    {
        foreach (var bookmarkDisplay in renderedBookmarks)
            SetBookmarkPos(bookmarkDisplay.Text.rectTransform, bookmarkDisplay.MapBookmark.SongBpmTime);
    }

    private void HandleBPMChangeRefreshed()
    {
        HandleEditorScaleChanged(0f);
        RefreshVisibility();
    }

    private void SetBookmarkPos(RectTransform rect, float songBpmTime)
    {
        //Need anchoredPosition3D, so Z gets precisely set, otherwise text might get under lighting grid
        rect.anchoredPosition3D = new Vector3(-4.5f, songBpmTime * EditorScaleController.EditorScale, 0);
    }

    private TextMeshProUGUI CreateGridBookmark(BaseBookmark bookmark)
    {
        var obj = new GameObject("GridBookmark", typeof(TextMeshProUGUI));
        obj.SetActive(false);
        var rect = (RectTransform)obj.transform;
        rect.SetParent(gridBookmarksParent);
        SetBookmarkPos(rect, bookmark.SongBpmTime);
        rect.sizeDelta = Vector2.one;
        rect.localRotation = Quaternion.identity;

        var text = obj.GetComponent<TextMeshProUGUI>();
        text.font = PersistentUI.Instance.ButtonPrefab.Text.font;
        text.alignment = TextAlignmentOptions.Left;
        text.fontSize = 0.4f;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.fontMaterial.renderQueue = 3150; // Above grid and measure numbers - Below grid interface
        SetGridBookmarkNameColor(text, bookmark.Color, bookmark.Name);

        return text;
    }

    private void RefreshBookmarkGridLine(object _)
    {
        foreach (var cachedBookmark in renderedBookmarks)
            SetGridBookmarkNameColor(cachedBookmark.Text, cachedBookmark.Color, cachedBookmark.Name);
    }


    private void SetGridBookmarkNameColor(TextMeshProUGUI text, Color color, string name)
    {
        var hex = HEXFromColor(color, false);

        SetText();
        text.ForceMeshUpdate();

        //Here making so bookmarks with short name have still long colored rectangle on the right to the grid
        if (text.textBounds.size.x < 2) //2 is distance between notes and lighting grid
        {
            SetText(
                (int)((2 - text.textBounds.size.x) / 0.0642f)); //Divided by 'space' character width for chosen fontSize
        }

        void SetText(int spaceNumber = 0)
        {
            var spaces = spaceNumber <= 0 ? null : new string(' ', spaceNumber);
            //<voffset> to align the bumped up text to grid, <s> to draw a line across the grid, in the end putting transparent dot, so trailing spaces don't get trimmed, 
            text.text = (Settings.Instance.GridBookmarksHasLine)
                ? $"<mark={hex}50><voffset=0.06><s> <indent=3.92> </s></voffset> {name}{spaces}<color=#00000000>.</color>"
                : $"<mark={hex}50><voffset=0.06> <indent=3.92> </voffset> {name}{spaces}<color=#00000000>.</color>";
        }
    }

    /// <summary> Returned string starts with # </summary>
    private string HEXFromColor(Color color, bool inclAlpha = true) =>
        inclAlpha
            ? $"#{ColorUtility.ToHtmlStringRGBA(color)}"
            : $"#{ColorUtility.ToHtmlStringRGB(color)}";

    public void RefreshVisibility()
    {
        var currentSongBpmBeat = atsc.CurrentSongBpmTime;
        var songBpmBeatsAhead = frontNoteGridScaling.localScale.z / EditorScaleController.EditorScale;
        var songBpmBeatsBehind = songBpmBeatsAhead / 4f;

        foreach (var bookmarkDisplay in activeBookmarks.ToArray())
        {
            var time = bookmarkDisplay.MapBookmark.SongBpmTime;
            if (time >= currentSongBpmBeat - songBpmBeatsBehind && time <= currentSongBpmBeat + songBpmBeatsAhead)
                continue;

            bookmarkDisplay.Text.gameObject.SetActive(false);
            activeBookmarks.Remove(bookmarkDisplay);
        }

        // if only i can skip
        foreach (var bookmarkDisplay in renderedBookmarks)
        {
            var time = bookmarkDisplay.MapBookmark.SongBpmTime;
            if (time < currentSongBpmBeat - songBpmBeatsBehind) continue;
            if (time > currentSongBpmBeat + songBpmBeatsAhead) break;
            if (activeBookmarks.Contains(bookmarkDisplay)) continue;

            bookmarkDisplay.Text.gameObject.SetActive(true);
            activeBookmarks.Add(bookmarkDisplay);
        }
    }
}
