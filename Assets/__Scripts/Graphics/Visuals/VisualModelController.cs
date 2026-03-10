using System;
using System.Collections.Generic;
using System.Linq;
using CustomNotes;
using UnityEngine;

public class VisualModelController : VisualController
{
    public Transform ParentTransform;

    public event Action<Mesh, Transform> OnMeshChanged;
    public event Action<Mesh> OnColliderChanged;

    [Header("State")] public List<GameObject> Actives = new();
    public List<Renderer> Renderers = new();
    private readonly Dictionary<string, GameObject> instantiatedObjects = new();
    private readonly Queue<GameObject> cleanupQueue = new();
    public int MaxCache = 1;
    private bool hasInstantiated;
    private bool markReplace;

    public void Start()
    {
        foreach (var active in Actives.Where(active => !cleanupQueue.Contains(active)))
        {
            cleanupQueue.Enqueue(active);
            instantiatedObjects[active.name] = active;
        }

        hasInstantiated = true;
    }

    public override void OnValidate()
    {
        base.OnValidate();
        ParentTransform = transform;
    }

    public void Cleanup()
    {
        for (var i = 0; i < cleanupQueue.Count - MaxCache; i++)
        {
            var go = cleanupQueue.Dequeue();
            if (instantiatedObjects.TryGetValue(go.name, out var instance) && !instance.activeSelf)
            {
                instantiatedObjects.Remove(go.name);
                Destroy(go);
            }
            else if (go.activeSelf)
                cleanupQueue.Enqueue(go);
            else
                Destroy(go);
        }
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
        foreach (var active in Actives)
        {
            if (!hasInstantiated)
            {
                if (!cleanupQueue.Contains(active))
                {
                    cleanupQueue.Enqueue(active);
                    instantiatedObjects[active.name] = active;
                }
            }

            active.SetActive(false);
        }

        Cleanup();
        Actives.Clear();
        Renderers.Clear();
        markReplace = true;
    }

    public void Add(VisualModelSO vm) => Add(vm.Prefab, vm.Collider, vm.name);

    public void Add(PrimitiveType type)
    {
        var shapeName = type.ToString();
        GameObject g;
        if (instantiatedObjects.TryGetValue(shapeName, out var instance) && !instance.activeSelf)
            g = instance;
        else
        {
            g = GameObject.CreatePrimitive(type);
            g.transform.SetParent(ParentTransform);
            cleanupQueue.Enqueue(g);
            instantiatedObjects[shapeName] = g;
        }

        AddInstanced(g, g.GetComponent<MeshFilter>().sharedMesh, shapeName);
    }

    public void Add(GameObject go, Mesh collMesh, string instanceName)
    {
        GameObject g;
        if (instantiatedObjects.TryGetValue(instanceName, out var instance) && !instance.activeSelf)
            g = instance;
        else
        {
            g = Instantiate(go, ParentTransform);
            cleanupQueue.Enqueue(g);
            instantiatedObjects[instanceName] = g;
        }

        AddInstanced(g, collMesh, instanceName);
    }

    private void AddInstanced(GameObject instance, Mesh collMesh, string instanceName)
    {
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
