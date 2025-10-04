using UnityEngine;

/// <summary>
/// Base class for a single component of an environment object. The class itself is simply a data type used for deserialization, however provides a method to apply its properties to a Unity / ChroMapper component.
/// </summary>
/// <typeparam name="T">Unity / ChroMapper component to copy data to.</typeparam>
public abstract class EnvironmentComponent<T> where T : Object
{
    /// <summary>
    /// Copies the properties of this EnvironmentComponent to the target Unity / ChroMapper component.
    /// It is assumed that the target component is freshly instantiated and does not have any properties set.
    /// </summary>
    public abstract void CopyTo(T target);
}
