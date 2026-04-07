public class SpectrogramComponent : EnvDataComponent<Spectrogram>
{
    public bool SetAsGlobal;
    public string[] MeshRenderers;
    public string MaterialPropertyBlockController;

    public override void CopyTo(Spectrogram target) => target.SetAsGlobal = SetAsGlobal;
}
