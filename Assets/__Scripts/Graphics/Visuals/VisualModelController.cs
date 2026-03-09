using System;
using System.Collections.Generic;
using System.Linq;
using CustomNotes;
using UnityEngine;

// TODO: cache or set deactive instead of removing the prefab entirely on change
public class VisualModelController : VisualController
{
    public Transform ParentTransform;

    private bool markReplace;
    public event Action<Mesh, Transform> OnMeshChanged;
    public event Action<Mesh> OnColliderChanged;

    [Header("State")] public List<GameObject> Actives = new();
    public List<Renderer> Renderers = new();

    public override void OnValidate()
    {
        base.OnValidate();
        ParentTransform = transform;
    }

    public void Set(VisualModelSO vm)
    {
        if (Actives.Count == 1 && Actives.Exists(x => x.gameObject.name == vm.name)) return;
        HandleReset();
        Add(vm);
    }

    public void Set(PrimitiveType type)
    {
        if (Actives.Count == 1 && Actives.Exists(x => x.gameObject.name == type.ToString())) return;
        HandleReset();
        Add(type);
    }

    public void Set(GameObject go, Mesh collMesh, string instanceName)
    {
        if (Actives.Count == 1 && Actives.Exists(x => x.gameObject.name == instanceName)) return;
        HandleReset();
        Add(go, collMesh, instanceName);
    }

    private void HandleReset()
    {
        MpbController.Remove(Renderers);
        Actives.ForEach(GameObjectExtensions.DestroySafe);
        Actives.Clear();
        Renderers.Clear();
        markReplace = true;
    }

    public void Add(VisualModelSO vm) => AddInstanced(Instantiate(vm.Prefab), vm.Collider, vm.name);

    public void Add(PrimitiveType type)
    {
        var obj = GameObject.CreatePrimitive(type);
        AddInstanced(obj, obj.GetComponent<MeshFilter>().sharedMesh, type.ToString());
    }

    public void Add(GameObject go, Mesh collMesh, string instanceName) =>
        AddInstanced(Instantiate(go), collMesh, instanceName);

    public void AddInstanced(GameObject instance, Mesh collMesh, string instanceName)
    {
        instance.transform.SetParent(ParentTransform, false);
        instance.name = instanceName;
        instance.SetActive(true);
        Actives.Add(instance);

        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        if (markReplace)
        {
            var meshFilter = renderers.First().GetComponentInChildren<MeshFilter>();
            if (meshFilter != null) OnMeshChanged?.Invoke(meshFilter.sharedMesh, meshFilter.transform);
            OnColliderChanged?.Invoke(collMesh);
            markReplace = false;
        }

        renderers = renderers.Where(r => r.GetComponent<DisableNoteColorOnGameobject>() == null).ToArray();

        Renderers.AddRange(renderers);
        MpbController.Add(renderers);
        MpbController.ApplyChanges();
    }
}
