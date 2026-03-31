using System;
using UnityEngine;

public class TransformSpectrogram : MonoBehaviour
{
    public enum LightAxis
    {
        X = 0,
        Y = 1,
        Z = 2
    }
    [SerializeField] private AudioLink.AudioLink audioLink;
    [SerializeField]
    private Transform[] transforms;
    [SerializeField]
    private LightAxis axis = LightAxis.Y;
    [SerializeField]
    private float minPosition;
    [SerializeField]
    private float maxPosition;
    [SerializeField]
    private bool scaleSamples = true;
    [SerializeField]
    private float scale = 1f;
    private Vector3 direction;
    private Vector3[] defaultPositions;
    private const int WIDTH = 128;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Vector3 a = Vector3.zero;
        if (audioLink == null)
            audioLink = FindObjectOfType<AudioLink.AudioLink>();

        if (audioLink == null)
        {
            Debug.LogError("AudioLink not found in scene!");
            return;
        }
        audioLink.EnableReadback();
        switch (axis)
        {
            case LightAxis.X:
                direction = new Vector3(1f, 0.0f, 0.0f);
                a = new Vector3(0.0f, 1f, 1f);
                break;
            case LightAxis.Y:
                direction = new Vector3(0.0f, 1f, 0.0f);
                a = new Vector3(1f, 0.0f, 1f);
                break;
            case LightAxis.Z:
                direction = new Vector3(0.0f, 0.0f, 1f);
                a = new Vector3(1f, 1f, 0.0f);
                break;
        }
        defaultPositions = new Vector3[transforms.Length];
        for (int index = 0; index < transforms.Length; ++index)
            defaultPositions[index] = Vector3.Scale(a, transforms[index].localPosition);
    }

    // Update is called once per frame
    void Update()
    {
        if (audioLink.audioData == null) return;
        for (int i = 0; i < transforms.Length; i++)
        {
            int band;

            if (scaleSamples)
            {
                float scaled = (float)i / (transforms.Length - 1f) * 127f;
                band = Mathf.RoundToInt(scaled * scale) % 128;
            }
            else
            {
                band = i % 128;
            }
            int index = 4 * WIDTH + band;
            float sample = (audioLink.audioData[index].b)*2;
            float value = Mathf.Lerp(minPosition, maxPosition, sample);
            transforms[i].localPosition =
                defaultPositions[i] + direction * value;
        }
    }
#if UNITY_EDITOR
    [ContextMenu("Populate Transforms From Children")]
    private void PopulateTransformsFromChildren()
    {
        transforms = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            transforms[i] = transform.GetChild(i);
        }
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"Populated {transforms.Length} transforms.");
    }
#endif
}
