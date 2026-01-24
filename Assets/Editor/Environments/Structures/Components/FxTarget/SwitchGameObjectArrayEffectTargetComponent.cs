public class SwitchGameObjectArrayEffectTargetComponent
{
    public GameObjectActivation[] GameObjects;

    public struct GameObjectActivation
    {
        public float Threshold;
        public string GameObject;
    }
}
