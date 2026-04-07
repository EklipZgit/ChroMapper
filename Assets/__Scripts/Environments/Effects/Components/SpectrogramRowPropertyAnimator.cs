using System.Linq;
using UnityEngine;

public class SpectrogramRowPropertyAnimator : MonoBehaviour
{
    [SerializeField] public MaterialPropertyBlockController MpbController;
    [SerializeField] public int DataIndex;
    [SerializeField] public string PropertyName;
    [SerializeField] public float Multiplier = 5f;
    [SerializeField] public AnimationCurve AnimationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] public AudioLink.AudioLink AudioLink;

    private const int audioLinkWidth = 128;
    private const int spectrogramStartRow = 4;
    private int propertyId;
    private bool isInitialized;
    private float spectrogramValue;

    private void Awake()
    {
        if (AudioLink == null)
            AudioLink = FindObjectsByType<AudioLink.AudioLink>(FindObjectsSortMode.None).FirstOrDefault();

        if (AudioLink == null)
        {
            Debug.LogError("AudioLink not found!");
            enabled = false;
            return;
        }

        AudioLink.audioDataToggle = true;
        AudioLink.EnableReadback();
        LazyInit();
        enabled = MpbController != null;
    }

    private void Update()
    {
        var index = (spectrogramStartRow * audioLinkWidth) + (DataIndex % audioLinkWidth);
        var sample = AudioLink.audioData[index].b * 2;

        spectrogramValue = Multiplier * AnimationCurve.Evaluate(sample);
        SetProperty();
        MpbController.ApplyChanges();
    }

    private void LazyInit()
    {
        if (!isInitialized)
        {
            isInitialized = true;
            propertyId = Shader.PropertyToID(PropertyName);
        }
    }

    private void SetProperty() => MpbController.Mpb.SetFloat(propertyId, spectrogramValue);

    public void SetMultiplier(float value) => Multiplier = value;
}
