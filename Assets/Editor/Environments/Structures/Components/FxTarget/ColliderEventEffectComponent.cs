public class ColliderEventEffectComponent
{
    public bool IsEnabled;
    
    public string EffectCollider;
    public float Value;

    public void CopyTo(ColliderFx target) => target.Value = Value;
}
