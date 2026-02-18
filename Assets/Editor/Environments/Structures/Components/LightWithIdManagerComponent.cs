using System.Collections.Generic;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class LightWithIdManagerComponent
{
    public Dictionary<int, LightId[]> Lights;

    public class LightId
    {
        public string ObjectId;
        public int InstanceId;
        public int? ArrayId;
    }
}
