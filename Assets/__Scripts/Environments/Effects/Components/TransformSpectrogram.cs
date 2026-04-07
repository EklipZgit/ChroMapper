using System;
using System.Linq;
using Beatmap.Enums;
using UnityEngine;

public class TransformSpectrogram : MonoBehaviour
{
    [SerializeField] public AudioLink.AudioLink AudioLink;
    [SerializeField] public Transform[] Transforms;
    [SerializeField] public Axis Axis = Axis.Y;
    [SerializeField] public float MinPosition;
    [SerializeField] public float MaxPosition;
    [SerializeField] public bool ScaleSamples = true;
    [SerializeField] public float Scale = 1f;

    private Vector3 direction;
    private Vector3[] defaultPositions;

    private const int width = 128;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        var a = Vector3.zero;
        if (AudioLink == null)
            AudioLink = FindObjectsByType<AudioLink.AudioLink>(FindObjectsSortMode.None).FirstOrDefault();

        if (AudioLink == null)
        {
            Debug.LogError("AudioLink not found in scene!");
            return;
        }

        AudioLink.EnableReadback();
        switch (Axis)
        {
            case Axis.X:
                direction = new Vector3(1f, 0.0f, 0.0f);
                a = new Vector3(0.0f, 1f, 1f);
                break;
            case Axis.Y:
                direction = new Vector3(0.0f, 1f, 0.0f);
                a = new Vector3(1f, 0.0f, 1f);
                break;
            case Axis.Z:
                direction = new Vector3(0.0f, 0.0f, 1f);
                a = new Vector3(1f, 1f, 0.0f);
                break;
        }

        defaultPositions = new Vector3[Transforms.Length];
        for (var index = 0; index < Transforms.Length; ++index)
            defaultPositions[index] = Vector3.Scale(a, Transforms[index].localPosition);
    }

    // Update is called once per frame
    private void Update()
    {
        if (AudioLink.audioData == null) return;
        for (var i = 0; i < Transforms.Length; i++)
        {
            int band;

            if (ScaleSamples)
            {
                var scaled = i / (Transforms.Length - 1f) * 127f;
                band = Mathf.RoundToInt(scaled * Scale) % 128;
            }
            else
            {
                band = i % 128;
            }

            var index = (4 * width) + band;
            var sample = AudioLink.audioData[index].b * 2;
            var value = Mathf.Lerp(MinPosition, MaxPosition, sample);
            Transforms[i].localPosition =
                defaultPositions[i] + (direction * value);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Populate Transforms From Children")]
    private void PopulateTransformsFromChildren()
    {
        Transforms = new Transform[transform.childCount];
        for (var i = 0; i < transform.childCount; i++)
        {
            Transforms[i] = transform.GetChild(i);
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"Populated {Transforms.Length} transforms.");
    }
#endif
}
