using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GLSEventTrack : MonoBehaviour
{
    [SerializeField] public GridLane GridLane;
    [SerializeField] public Track Track;
    [SerializeField] public TrackDefinitionGLS TrackDefinition;

    [Header("Prefab")] [SerializeField] private Transform canvasTransform;
    [SerializeField] private TextMeshProUGUI textPrefab;
    private readonly List<TextMeshProUGUI> texts = new();

    public void SetText(TrackDefinitionGLS track)
    {
        foreach (var t in texts) t.enabled = false;
        var offset = 0;

        AddLabel(track.Name);

        if (track.ColorTrack) AddLabel("C");
        if (track.RotationTracks.Any(x => x)) AddLabel("R");
        if (track.TranslationTracks.Any(x => x)) AddLabel("T");
        if (track.FloatFXTrack) AddLabel("Fx");

        GridLane.Lane = offset - 1;

        return;

        void AddLabel(string n)
        {
            var label = GetOrCreateText();
            label.enabled = true;
            label.text = n;
            label.rectTransform.localPosition = Vector3.down * offset++;
        }
    }

    private TextMeshProUGUI GetOrCreateText()
    {
        foreach (var t in texts)
        {
            if (!t.enabled) return t;
        }

        var text = Instantiate(textPrefab, canvasTransform);
        texts.Add(text);
        return text;
    }
}
