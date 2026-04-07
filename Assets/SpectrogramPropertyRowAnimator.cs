using UnityEngine;

public class SpectrogramPropertyRowAnimator : MonoBehaviour
{
    [SerializeField]
    protected MaterialPropertyBlockController materialPropertyBlockController;
    [SerializeField]
    private int dataIndex;
    [SerializeField]
    private string propertyName;
    [SerializeField]
    private float multiplier = 5f;
    [SerializeField]
    private AnimationCurve animationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AudioLink.AudioLink audioLink;
    private const int AudioLinkWidth = 128;
    private const int SpectrogramStartRow = 4;
    private int propertyId;
    private bool isInitialized;
    private float spectrogramValue;

    // Update is called once per frame
    void SetProperty()
    {
        materialPropertyBlockController.Mpb.SetFloat(propertyId, spectrogramValue);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (audioLink == null)
            audioLink = FindObjectOfType<AudioLink.AudioLink>();

        if (audioLink == null)
        {
            Debug.LogError("AudioLink not found!");
            enabled = false;
            return;
        }

        audioLink.audioDataToggle = true;
        audioLink.EnableReadback();
        LazyInit();
        enabled = materialPropertyBlockController != null;
    }

    void Update()
    {
        if (audioLink.audioData == null) return;

        int index = SpectrogramStartRow * AudioLinkWidth + (dataIndex % AudioLinkWidth);

        float sample = audioLink.audioData[index].b * 2;

        spectrogramValue = multiplier * animationCurve.Evaluate(sample);
        SetProperty();
        materialPropertyBlockController.ApplyChanges();
    }
    void LazyInit()
    {
        if (!isInitialized)
        {
            isInitialized = true;
            propertyId = Shader.PropertyToID(propertyName);
        }
        
    }
    public void SetMultiplier(float value) => multiplier = value;
#if UNITY_EDITOR
    void OnValidate()
    {
        if (materialPropertyBlockController == null)
            materialPropertyBlockController = GetComponent<MaterialPropertyBlockController>();

        if (audioLink == null)
            audioLink = FindObjectOfType<AudioLink.AudioLink>();
    }
#endif
}
