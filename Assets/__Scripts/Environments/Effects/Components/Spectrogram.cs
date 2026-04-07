using UnityEngine;

public class Spectrogram : MonoBehaviour
{
    [SerializeField] public bool SetAsGlobal;
    [SerializeField] public MeshRenderer[] MeshRenderers;
    [SerializeField] public MaterialPropertyBlockController MpbController;

    private readonly AudioLink.AudioLink audioLink;
    private static readonly int spectrogramDataID = Shader.PropertyToID("_SpectrogramData");
    private static MaterialPropertyBlock materialPropertyBlock;

    private MaterialPropertyBlock MaterialPropertyBlock
    {
        get
        {
            if (!(MpbController != null)) return materialPropertyBlock;
            return MpbController.Mpb;
        }
    }

    protected void Awake()
    {
        if (!SetAsGlobal && MpbController == null && materialPropertyBlock == null)
            materialPropertyBlock = new MaterialPropertyBlock();
    }

    protected void Update()
    {
        // var sample = audioLink.audioData[index].b * 2;
        if (SetAsGlobal)
        {
            // Shader.SetGlobalFloatArray(spectrogramDataID, samples);
            return;
        }

        // MaterialPropertyBlock.SetFloatArray(spectrogramDataID, samples);
        if (MpbController != null)
        {
            MpbController.ApplyChanges();
            return;
        }

        foreach (var t in MeshRenderers) t.SetPropertyBlock(materialPropertyBlock);
    }
}
