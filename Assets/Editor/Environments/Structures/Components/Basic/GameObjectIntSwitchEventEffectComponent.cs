public class GameObjectIntSwitchEventEffectComponent
{
    public bool IsEnabled;

    public string EventType;
    public int DefaultValue;
    public GameObjectsValue[] GameObjectsValueLists;

    public class GameObjectsValue
    {
        public int Value;
        public string[] GameObjectIds;
    }

    public void CopyTo(GameObjectIntSwitch target)
    {
        target.DefaultValue = DefaultValue;
    }
}
