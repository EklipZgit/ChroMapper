using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beatmap.Info;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SongListItem : RecyclingListViewItem, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private const int CjkRasterizationScale = 2;

    private const float CjkTitleVerticalBleed = 2f;

    private static readonly Dictionary<string, WeakReference<Sprite>> cache = new();

    private static readonly Dictionary<string, float> durationCache = new();
    private static bool hasAppliedThisFrame;

    private static string durationCachePath;
    private static JSONObject songCoreCache;

    private static bool saveRunning;

    private static readonly byte[] oggBytes = { 0x4F, 0x67, 0x67, 0x53, 0x00, 0x04 };
    private static readonly byte[] vorbisBytes = { 0x76, 0x6F, 0x72, 0x62, 0x69, 0x73 };

    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI artist;
    [SerializeField] private TextMeshProUGUI folder;

    [SerializeField] private Font cjkFont;

    [SerializeField] private TextMeshProUGUI duration;
    [SerializeField] private TextMeshProUGUI bpm;
    [SerializeField] private Image favouritePreviewImage;

    [SerializeField] private Image cover;
    [SerializeField] private Sprite defaultCover;

    [SerializeField] private GameObject rightPanel;
    [SerializeField] private Toggle favouriteToggle;
    private Image bg;

    private bool ignoreToggle;
    private string previousSearch = "";

    private BaseInfo mapInfo;

    private SongList songList;

    private Text cjkTitle;
    private Text cjkArtist;
    private Text cjkFolder;

    private void Awake()
    {
        cjkTitle = CreateCjkRenderer(title, CjkTitleVerticalBleed);
        cjkArtist = CreateCjkRenderer(artist, 0f);
        cjkFolder = CreateCjkRenderer(folder, 0f);
    }

    private void Start()
    {
        rightPanel.SetActive(false);
        bg = GetComponent<Image>();
        // I have sinned
        songList = FindAnyObjectByType<SongList>();

        InitCache();
    }

    private static void InitCache()
    {
        if (songCoreCache != null) return;
        durationCachePath = PathUtils.Combine(Settings.Instance.BeatSaberInstallation, "UserData", "SongCore",
            "SongDurationCache.dat");
        if (!File.Exists(durationCachePath))
        {
            songCoreCache = new JSONObject();
            return;
        }

        try
        {
            using (var reader = new StreamReader(durationCachePath))
            {
                songCoreCache = JSON.Parse(reader.ReadToEnd()).AsObject;
                foreach (var keyValuePair in songCoreCache)
                    durationCache[Path.GetFullPath(keyValuePair.Key)] = keyValuePair.Value["duration"].AsFloat;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error trying to read from file {durationCachePath}\n{e}");
        }
    }

    private void Update() => hasAppliedThisFrame = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (BeatSaberSongContainer.Instance != null && mapInfo != null)
            BeatSaberSongContainer.Instance.SelectSongForEditing(mapInfo);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rightPanel.SetActive(true);
        bg.color = new Color(0.35f, 0.35f, 0.36f, 1);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rightPanel.SetActive(false);
        bg.color = new Color(0.31f, 0.31f, 0.31f, 1);
    }

    private string HighlightSubstring(string s, string search)
    {
        var stripped = s.StripTMPTags();
        var idx = stripped.IndexOf(search, StringComparison.InvariantCultureIgnoreCase);
        return idx >= 0 && search.Length > 0
            ? stripped.Substring(0, idx) + "<color=#ff0000ff>" + stripped.Substring(idx, search.Length) + "</color>" +
              stripped.Substring(idx + search.Length)
            : stripped;
    }

    private static bool ContainsCjk(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            var codePoint = char.ConvertToUtf32(value, i);
            if (char.IsHighSurrogate(value[i]))
            {
                i++;
            }

            if ((codePoint >= 0x2E80 && codePoint <= 0x4DBF)
                || (codePoint >= 0x4E00 && codePoint <= 0x9FFF)
                || (codePoint >= 0xAC00 && codePoint <= 0xD7AF)
                || (codePoint >= 0xF900 && codePoint <= 0xFAFF)
                || (codePoint >= 0xFF65 && codePoint <= 0xFF9F)
                || (codePoint >= 0x20000 && codePoint <= 0x2FA1F))
            {
                return true;
            }
        }

        return false;
    }

    private Text CreateCjkRenderer(TextMeshProUGUI source, float verticalBleed)
    {
        var clipGo = new GameObject($"{source.name} CJK Clip", typeof(RectTransform), typeof(RectMask2D));
        clipGo.layer = source.gameObject.layer;
        clipGo.transform.SetParent(source.transform, false);

        var clipRect = clipGo.GetComponent<RectTransform>();
        clipRect.anchorMin = Vector2.zero;
        clipRect.anchorMax = Vector2.one;
        clipRect.anchoredPosition = Vector2.zero;
        clipRect.sizeDelta = new Vector2(0f, verticalBleed * 2f);

        var go = new GameObject($"{source.name} CJK", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.layer = source.gameObject.layer;
        go.transform.SetParent(clipGo.transform, false);

        var rect = go.GetComponent<RectTransform>();
        var anchorExtent = CjkRasterizationScale / 2f;
        rect.anchorMin = Vector2.one * (0.5f - anchorExtent);
        rect.anchorMax = Vector2.one * (0.5f + anchorExtent);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one / CjkRasterizationScale;

        var renderer = go.GetComponent<Text>();
        renderer.font = cjkFont;
        renderer.fontSize = Mathf.RoundToInt(source.fontSize * CjkRasterizationScale);
        renderer.color = source.color;
        renderer.alignment = TextAnchor.MiddleLeft;
        renderer.horizontalOverflow = HorizontalWrapMode.Overflow;
        renderer.verticalOverflow = VerticalWrapMode.Overflow;
        renderer.supportRichText = true;
        renderer.raycastTarget = false;
        renderer.gameObject.SetActive(false);
        return renderer;
    }

    private static void SetMetadataText(
        TextMeshProUGUI tmpRenderer,
        Text cjkRenderer,
        string detectionText,
        string tmpText,
        string cjkText)
    {
        var useCjkRenderer = ContainsCjk(detectionText);
        tmpRenderer.text = tmpText;
        tmpRenderer.enabled = !useCjkRenderer;
        cjkRenderer.text = cjkText;
        cjkRenderer.gameObject.SetActive(useCjkRenderer);
    }

    // Deployed-build CJK rows keep their sub-mesh geometry but render nothing (log: mat=null with
    // verts>0). TMP's runtime fallback materials are referenced only by managed caches inside
    // TMP_MaterialManager, so once the last sub-mesh drops its reference the next asset collection
    // (UnloadUnusedAssets runs on every scene load) destroys the material while TMP's cache keeps
    // handing the corpse back to every later mesh rebuild — and the fake-null == check in the
    // fallbackMaterial setter permanently blocks re-registration. Pinning each live material keeps
    // both a Unity-side reference and a permanent counted ref so it cannot die by either path, and
    // SetMaterialDirty rebinds materialForRendering so a stale render binding is refreshed.
    // SongListSubMeshMaterialSurvivesFallbackCleanup
    private static void RefreshSubMeshMaterials(TextMeshProUGUI field)
    {
        foreach (var sm in field.GetComponentsInChildren<TMP_SubMeshUI>(true))
        {
            if (sm.sharedMaterial != null)
                TMPFallbackMaterialHolder.Pin(sm.sharedMaterial);
            sm.SetMaterialDirty();
        }
    }

    // Rows are recycled via SetActive(false)/(true) in RecyclingListView, and AssignSong
    // early-returns when a recycled row shows the same map — that path never reaches the
    // coroutine refresh, so a fallback material released by TMP_SubMeshUI.OnDisable would stay
    // unpinned. Re-pinning here lifts its refcount back above zero before the next
    // willRenderCanvases sweep and marks the render material dirty for rebinding.
    // SongListSubMeshMaterialSurvivesFallbackCleanup
    private void OnEnable()
    {
        RefreshSubMeshMaterials(title);
        RefreshSubMeshMaterials(artist);
        RefreshSubMeshMaterials(folder);
    }

    public void AssignSong(BaseInfo mapInfo, string searchFieldText)
    {
        if (this.mapInfo == mapInfo && previousSearch == searchFieldText) return;

        StopCoroutine(nameof(LoadImage));
        StopCoroutine(nameof(LoadDuration));

        previousSearch = searchFieldText;
        this.mapInfo = mapInfo;
        var songName = HighlightSubstring(mapInfo.SongName, searchFieldText);
        var artistName = HighlightSubstring(mapInfo.SongAuthorName, searchFieldText);

        var subName = mapInfo.SongSubName.StripTMPTags();
        var tmpTitle = $"{songName} <size=50%><i>{subName}</i></size>";
        var cjkTitleText = string.IsNullOrEmpty(subName)
            ? songName
            : $"{songName} <size={Mathf.Max(1, cjkTitle.fontSize / 2)}><i>{subName}</i></size>";
        SetMetadataText(title, cjkTitle, mapInfo.SongName + mapInfo.SongSubName, tmpTitle, cjkTitleText);
        SetMetadataText(artist, cjkArtist, mapInfo.SongAuthorName, artistName, artistName);
        SetMetadataText(folder, cjkFolder, mapInfo.Directory, mapInfo.Directory, mapInfo.Directory);

        duration.text = "-:--";
        bpm.text = $"{mapInfo.BeatsPerMinute:N0}";

        ignoreToggle = true;
        favouriteToggle.isOn = this.mapInfo.IsFavourite;
        favouritePreviewImage.gameObject.SetActive(this.mapInfo.IsFavourite);
        ignoreToggle = false;

        StartCoroutine(nameof(LoadImage));

        if (mapInfo.SongDurationMetadata > 0)
        {
            SetDuration(mapInfo.SongDurationMetadata);
        }
        else
        {
            StartCoroutine(nameof(LoadDuration));
        }
    }

    private IEnumerator LoadImage()
    {
        var fullPath = PathUtils.Combine(mapInfo.Directory, mapInfo.CoverImageFilename);

        if (cache.TryGetValue(fullPath, out var spriteRef) && spriteRef.TryGetTarget(out var existingSprite))
        {
            cover.sprite = existingSprite;
            yield break;
        }

        cover.sprite = defaultCover;
        if (!File.Exists(fullPath)) yield break;

        var uriPath = Application.platform is RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor
            ? Uri.EscapeDataString(fullPath)
            : Uri.EscapeUriString(fullPath);

        var www = UnityWebRequestTexture.GetTexture($"file:///{uriPath}");
        
        yield return www.SendWebRequest();

        // Copying the texture generates mipmaps for better scaling
        var newTex = ((DownloadHandlerTexture)www.downloadHandler).texture;

        if (newTex == null)
        {
            Debug.LogWarning("Cover image file exists but the texture failed to load.");
            yield break;
        }

        newTex.wrapMode = TextureWrapMode.Clamp;

        // Only allow one sprite to be created per frame to reduce stuttering
        while (hasAppliedThisFrame)
            yield return new WaitForEndOfFrame();

        hasAppliedThisFrame = true;

        var sprite = Sprite.Create(newTex, new Rect(0, 0, newTex.width, newTex.height), new Vector2(0, 0), 100f);
        cover.sprite = sprite;
        cache[fullPath] = new WeakReference<Sprite>(sprite);
    }

    private void SetDuration(string path, float length) => SetDuration(this, path, length);

    public static void SetDuration(MonoBehaviour crTarget, string path, float length)
    {
        InitCache();

        var songCoreCacheObj = songCoreCache.GetValueOrDefault(path, new JSONObject { ["id"] = "CMCachedDuration" });
        songCoreCacheObj["duration"] = length;
        songCoreCache.Add(path, songCoreCacheObj);
        crTarget.StartCoroutine(SaveCachedDurations());

        durationCache[path] = length;
        if (crTarget is SongListItem item) item.SetDuration(length);
    }

    private static IEnumerator SaveCachedDurations()
    {
        if (saveRunning) yield break;
        saveRunning = true;

        if (!File.Exists(durationCachePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(durationCachePath) ??
                                      throw new InvalidOperationException("Directory was null?"));
        }

        yield return new WaitForSeconds(3);
        using (var writer = new StreamWriter(durationCachePath, false))
        {
            writer.Write(songCoreCache.ToString());
        }

        saveRunning = false;
    }

    private void SetDuration(float length)
    {
        var totalSeconds = Mathf.RoundToInt(length);
        var mins = totalSeconds / 60;
        var seconds = totalSeconds % 60;

        duration.text = $"{mins}:{seconds:D2}";
    }

    private IEnumerator LoadDuration()
    {
        var cacheKey = Path.GetFullPath(mapInfo.Directory);
        var fullPath = PathUtils.Combine(mapInfo.Directory, mapInfo.SongFilename);

        if (!File.Exists(fullPath)) yield break;

        yield return null;

        if (durationCache.TryGetValue(cacheKey, out var cachedDuration) && cachedDuration >= 0)
        {
            SetDuration(cachedDuration);
            yield break;
        }

        var oggLength = GetLengthFromOgg(fullPath);
        if (oggLength >= 0)
        {
            SetDuration(cacheKey, oggLength);
            yield break;
        }

        yield return BeatSaberSongExtensions.LoadAudio(mapInfo, (clip) => SetDuration(cacheKey, clip.length), 0, null);
    }

    private static bool FindBytes(Stream fs, BinaryReader br, byte[] bytes, int searchLength)
    {
        for (var i = 0; i < searchLength; i++)
        {
            var b = br.ReadByte();
            if (b != bytes[0]) continue;
            var by = br.ReadBytes(bytes.Length - 1);
            // hardcoded 6 bytes compare, is fine because all inputs used are 6 bytes
            // bitwise AND the last byte to read only the flag bit for lastSample searching
            // shouldn't cause issues finding rate, hopefully
            if (by[0] == bytes[1] && by[1] == bytes[2] && by[2] == bytes[3] && by[3] == bytes[4] &&
                (by[4] & bytes[5]) == bytes[5])
            {
                return true;
            }

            var index = Array.IndexOf(by, bytes[0]);
            if (index != -1)
            {
                fs.Position += index - (bytes.Length - 1);
                i += index;
            }
            else
            {
                i += bytes.Length - 1;
            }
        }

        return false;
    }

    public static float GetLengthFromOgg(string oggFile)
    {
        using (var fs = File.OpenRead(oggFile))
        using (var br = new BinaryReader(fs, Encoding.ASCII))
        {
            //Skip Capture Pattern
            fs.Position = 24;

            if (!FindBytes(fs, br, vorbisBytes, 256))
            {
                Debug.Log($"Could not find rate for {oggFile}");
                return -1;
            }

            fs.Position += 5;
            var rate = br.ReadInt32();
            long lastSample = -1;

            /*
             * this finds the last occurrence of OggS in the file by checking for a bit flag (0x04)
             * reads in blocks determined by seekBlockSize
             * 6144 does not add significant overhead and speeds up the search significantly
             */
            const int seekBlockSize = 6144;
            const int seekTries = 10; // 60 KiB should be enough for any sane ogg file
            for (var i = 0; i < seekTries; i++)
            {
                var seekPos = (i + 1) * seekBlockSize * -1;
                var overshoot = Math.Max((int)(-seekPos - fs.Length), 0);
                if (overshoot >= seekBlockSize)
                    break;

                fs.Seek(seekPos + overshoot, SeekOrigin.End);
                if (!FindBytes(fs, br, oggBytes, seekBlockSize - overshoot)) continue;

                lastSample = br.ReadInt64();
                break;
            }

            if (lastSample == -1)
            {
                Debug.Log($"Could not find lastSample for {oggFile}");
                return -1;
            }

            return lastSample / (float)rate;
        }
    }

    public void OnFavourite(bool isFavourite)
    {
        if (ignoreToggle) return;

        songList.RemoveSong(mapInfo);
        mapInfo.IsFavourite = isFavourite;
        favouritePreviewImage.gameObject.SetActive(isFavourite);
        songList.AddSong(mapInfo);
    }
}
