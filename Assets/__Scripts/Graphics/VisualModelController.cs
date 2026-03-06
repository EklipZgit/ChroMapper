using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VisualModelController : VisualController
{
    public VisualModelSO Default;
    public Transform ParentTransform;

    private bool markReplace;
    public event Action<Mesh> OnMeshChanged;
    public event Action<Mesh> OnColliderChanged;

    [Header("State")] public List<GameObject> Actives = new();
    public List<Renderer> Renderers = new();

    public override void OnValidate()
    {
        base.OnValidate();
        ParentTransform = transform;
    }

    public void Set(string n)
    {
        if (Actives.Count == 1 && Actives.Exists(x => x.gameObject.name == n)) return;
        HandleReset();
        Add(n);
    }

    public void Set(GameObject go, Mesh collMesh)
    {
        if (Actives.Count == 1 && Actives.Exists(x => x.gameObject.name == go.name)) return;
        HandleReset();
        Add(go, collMesh);
    }

    public void Set(PrimitiveType type)
    {
        if (Actives.Count == 1 && Actives.Exists(x => x.gameObject.name == type.ToString())) return;
        HandleReset();
        Add(type);
    }

    private void HandleReset()
    {
        MpbController.Remove(Renderers);
        Actives.ForEach(GameObjectExtensions.DestroySafe);
        Actives.Clear();
        Renderers.Clear();
        markReplace = true;
    }

    public void Add(string n)
    {
        var obj = Repository.ModelsByName.GetValueOrDefault(n, Default);
        Add(obj.Prefab, obj.Collider);
    }

    public void Add(PrimitiveType type)
    {
        var obj = GameObject.CreatePrimitive(type);
        AddInstanced(obj, obj.GetComponent<MeshFilter>().sharedMesh, type.ToString());
    }

    public void Add(GameObject go, Mesh collMesh) => AddInstanced(Instantiate(go), collMesh, go.name);

    public void AddInstanced(GameObject instance, Mesh collMesh, string instanceName)
    {
        instance.transform.SetParent(ParentTransform, false);
        instance.name = instanceName;
        Actives.Add(instance);

        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        if (markReplace)
        {
            OnMeshChanged?.Invoke(renderers.First().GetComponent<MeshFilter>().sharedMesh);
            if (collMesh != null) OnColliderChanged?.Invoke(collMesh);
            markReplace = false;
        }

        Renderers.AddRange(renderers);
        MpbController.Add(renderers);
        MpbController.ApplyChanges();
    }
}
