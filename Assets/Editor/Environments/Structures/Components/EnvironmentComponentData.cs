using System;
using UnityEngine;

public abstract class EnvironmentComponentData
{
    [NonSerialized] public Component Instance;
    public virtual int Priority => 0;

    public bool IsEnabled;
    public int InstanceId;

    public virtual bool AllowNew => true;

    /// <summary>Gets the exact Unity component type represented by this data record.</summary>
    public abstract Type ComponentType { get; }

    public abstract void Apply(CreateContainer container);

    /// <summary>Binds this record to a retained exact-type component, or creates its component.</summary>
    /// <param name="self">GameObject which owns the component.</param>
    /// <param name="retainedComponent">Previously serialized component to reuse, or <c>null</c>.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="retainedComponent"/> has the wrong type or belongs to another GameObject.
    /// </exception>
    public abstract void SpawnComponent(GameObject self, Component retainedComponent = null);
}

/// <summary>
/// Base class for a single component of an environment object. The class itself is simply a data type used for deserialization, however provides a method to apply its properties to a Unity / ChroMapper component.
/// </summary>
/// <typeparam name="T">Unity / ChroMapper component to copy data to.</typeparam>
public abstract class EnvironmentComponentData<T> : EnvironmentComponentData where T : Component
{
    public override Type ComponentType => typeof(T);

    public override void SpawnComponent(GameObject self, Component retainedComponent = null)
    {
        // EnvironmentData can be reused for more than one regeneration. Never let its
        // non-serialized runtime link select a component from an earlier pass.
        Instance = null;

        if (retainedComponent != null)
        {
            if (retainedComponent.GetType() != typeof(T) || retainedComponent.gameObject != self)
                throw new ArgumentException(
                    $"Expected a {typeof(T)} component owned by '{self.name}'.",
                    nameof(retainedComponent));

            Instance = retainedComponent;
            if (retainedComponent is Behaviour retainedBehaviour) retainedBehaviour.enabled = IsEnabled;
            return;
        }

        if (!AllowNew)
        {
            Instance = self.GetComponent<T>();
            return;
        }

        var comp = self.AddComponent<T>();
        if (comp is Behaviour b) b.enabled = IsEnabled;
        Instance = comp;
    }

    public override void Apply(CreateContainer container)
    {
        var comp = Instance as T;
        if (comp == null)
        {
            Debug.LogError($"{this} missing");
        }

        var self = comp.gameObject;
        FillComponents(self, comp, container);
    }

    public T GetComponent() => Instance as T;

    public abstract void FillComponents(GameObject self, T comp, CreateContainer container);
}
