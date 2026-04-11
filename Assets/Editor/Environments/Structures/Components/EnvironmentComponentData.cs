using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Base class for a single component of an environment object. The class itself is simply a data type used for deserialization, however provides a method to apply its properties to a Unity / ChroMapper component.
/// </summary>
/// <typeparam name="T">Unity / ChroMapper component to copy data to.</typeparam>
public abstract class EnvironmentComponentData<T> where T : Component
{
    public bool IsEnabled = true;
    [JsonProperty("instanceId")]public int InstanceId = -1;

    public T Apply(GameObject self, CreateContainer container)
    {
        var comp = self.AddComponent<T>();
        if (comp is Behaviour b) b.enabled = IsEnabled;
        SearchAndFillComponents(self, comp, container);
        CopyTo(comp);
        return comp;
    }

    public abstract void SearchAndFillComponents(GameObject self, T comp, CreateContainer container);

    /// <summary>
    /// Copies the properties of this EnvironmentComponent to the target Unity / ChroMapper component.
    /// It is assumed that the target component is freshly instantiated and does not have any properties set.
    /// </summary>
    public abstract void CopyTo(T comp);
}
