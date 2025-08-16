using UnityEngine;

public abstract class EnvironmentComponent<T> where T : Object
{
    /// <summary>
    /// Copies the properties of this EnvironmentComponent to the target Unity / ChroMapper component.
    /// It is assumed that the target component is freshly instantiated and does not have any properties set.
    /// </summary>
    public abstract void CopyTo(T target);
}
