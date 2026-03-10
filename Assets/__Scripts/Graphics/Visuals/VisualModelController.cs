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

    [Header("State")] public List<ModelData> Actives = new();
    public List<Renderer> Renderers = new();
    private readonly Dictionary<string, ModelData> nameToInstancedObjects = new();
    private readonly Queue<ModelData> cleanupQueue = new();
    public int MaxCache = 1;
    private bool hasInstantiated;
    private bool markReplace;

    public void Start()
    {
        foreach (var active in Actives.Where(active => !cleanupQueue.Contains(active)))
        {
            cleanupQueue.Enqueue(active);
            nameToInstancedObjects[active.Name] = active;
        }

        hasInstantiated = true;
    }

    public void OnValidate()
    {
        if (Application.isPlaying) return;
        ParentTransform = transform;
        for (var index = 0; index < Actives.Count; index++)
        {
            var active = Actives[index];
            active.Name = active.GameObject.name;
            Actives[index] = active;
        }
    }

    public void Cleanup()
    {
        for (var i = 0; i < cleanupQueue.Count - MaxCache; i++)
        {
            var data = cleanupQueue.Dequeue();
            if (nameToInstancedObjects.TryGetValue(data.Name, out var instance) && !instance.GameObject.activeSelf)
            {
                nameToInstancedObjects.Remove(data.Name);
                Destroy(data.GameObject);
            }
            else if (data.GameObject.activeSelf)
                cleanupQueue.Enqueue(data);
            else
                Destroy(data.GameObject);
        }
    }

    public void Set(VisualModelSO vm)
    {
        if (Actives.Count == 1 && Actives.Exists(x => x.Name == vm.Name)) return;
        HandleReset();
        Add(vm);
    }

    public void Set(PrimitiveType type)
    {
        if (Actives.Count == 1 && Actives.Exists(x => x.Name == type.ToString())) return;
        HandleReset();
        Add(type);
    }

    public void Set(GameObject go, Mesh collMesh, string instanceName)
    {
        if (Actives.Count == 1 && Actives.Exists(x => x.Name == instanceName)) return;
        HandleReset();
        Add(go, collMesh, instanceName);
    }

    private void HandleReset()
    {
        MpbController.Remove(Renderers);
        for (var index = 0; index < Actives.Count; index++)
        {
            var active = Actives[index];
            if (!hasInstantiated)
            {
                if (!cleanupQueue.Contains(active))
                {
                    cleanupQueue.Enqueue(active);
                    nameToInstancedObjects[active.Name] = active;
                }
            }

            active.GameObject.SetActive(false);
        }

        Cleanup();
        Actives.Clear();
        Renderers.Clear();
        markReplace = true;
    }

    public void Add(VisualModelSO vm) => Add(vm.Prefab, vm.Collider, vm.Name);

    public void Add(PrimitiveType type)
    {
        var shapeName = type.ToString();
        ModelData data;
        if (nameToInstancedObjects.TryGetValue(shapeName, out var instance) && !instance.GameObject.activeSelf)
            data = instance;
        else
        {
            data = new(shapeName, GameObject.CreatePrimitive(type));
            data.GameObject.transform.SetParent(ParentTransform);
            cleanupQueue.Enqueue(data);
            nameToInstancedObjects[shapeName] = data;
        }

        AddInstanced(data, data.GameObject.GetComponent<MeshFilter>().sharedMesh);
    }

    public void Add(GameObject go, Mesh collMesh, string instanceName)
    {
        ModelData data;
        if (nameToInstancedObjects.TryGetValue(instanceName, out var instance) && !instance.GameObject.activeSelf)
            data = instance;
        else
        {
            data = new(instanceName, Instantiate(go, ParentTransform));
            cleanupQueue.Enqueue(data);
            nameToInstancedObjects[instanceName] = data;
        }

        AddInstanced(data, collMesh);
    }

    private void AddInstanced(ModelData data, Mesh collMesh)
    {
        data.GameObject.SetActive(true);
        Actives.Add(data);

        if (data.Renderers.Length == 0) return;

        if (markReplace)
        {
            if (data.OutlineMesh != null)
                OnMeshChanged?.Invoke(data.OutlineMesh.sharedMesh, data.OutlineMesh.transform);
            OnColliderChanged?.Invoke(collMesh);
            markReplace = false;
        }

        Renderers.AddRange(data.MpbRenderers);
        MpbController.Add(data.MpbRenderers);
        MpbController.ApplyChanges();
    }
}

[Serializable]
public struct ModelData : IEquatable<ModelData>
{
    public string Name;
    public GameObject GameObject;

    public MeshFilter OutlineMesh;

    public Renderer[] Renderers;
    public Renderer[] MpbRenderers;

    public ModelData(string name, GameObject gameObject)
    {
        Name = name;
        GameObject = gameObject;
        GameObject.name = name;


        Renderers = gameObject.GetComponentsInChildren<Renderer>();
        OutlineMesh = Renderers.Length > 0 ? Renderers.First().GetComponentInChildren<MeshFilter>() : null;
        MpbRenderers = Renderers
            .Where(r => r.GetComponent<DisableNoteColorOnGameobject>() == null)
            .ToArray();
    }

    public bool Equals(ModelData other)
    {
        return Name == other.Name
            && Equals(GameObject, other.GameObject);
    }

    public override bool Equals(object obj) => obj is ModelData other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Name, GameObject);
}
