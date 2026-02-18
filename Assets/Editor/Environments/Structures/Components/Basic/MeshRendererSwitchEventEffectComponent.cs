public class MeshRendererSwitchEventEffectComponent
{
    public bool IsEnabled;

    public string EventType;
    public string[] ActivateOnBoostRenderers;
    public string[] DeactivateOnBoostRenderers;

    public void CopyTo(MeshRendererSwitch target)
    {
    }
}
