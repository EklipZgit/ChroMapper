using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Environment/Environment Mesh", fileName = "EnvironmentMeshSO")]
public class EnvironmentMeshSO : ScriptableObject
{
    [SerializeField] public List<MeshInfo> list = new();

    public readonly Dictionary<string, Mesh> Lookup = new();

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    private void Initialize()
    {
        Lookup.Clear();
        foreach (var entry in list) Lookup[entry.Hash] = entry.Mesh;
    }

    public void MarkForChange()
    {
        list.ForEach(x =>
        {
            x.Unused = true;
            x.Environments.Clear();
            x.Names.Clear();
        });
    }

    public void RemoveUnused() => list.RemoveAll(x => x.Unused);

    public void AddEntry(EnvInfoMesh mesh, string environment)
    {
        for (var index = 0; index < list.Count; index++)
        {
            var entry = list[index];
            if (entry.Hash != mesh.Hash) continue;

            entry.Unused = false;
            list[index] = entry;
        }

        if (list.All(x => x.Hash != mesh.Hash))
        {
            list.Add(
                new MeshInfo
                {
                    Hash = mesh.Hash,
                    Names = new List<string> { mesh.Name },
                    Environments = new List<string> { environment },
                    BoundsSize = mesh.BoundsSize,
                    BoundsCenter = mesh.BoundsCenter
                });
        }
        else
        {
            var m = list.First(x => x.Hash == mesh.Hash);
            if (!m.Names.Contains(mesh.Name)) m.Names.Add(mesh.Name);
            if (!m.Environments.Contains(environment)) m.Environments.Add(environment);
        }
    }

    public void Sort() => list.Sort((a, b) => string.Compare(a.Hash, b.Hash, StringComparison.Ordinal));
}

[Serializable]
public class MeshInfo
{
    public Mesh Mesh;
    public string Hash;
    public List<string> Names;
    public List<string> Environments;

    public Vector3 BoundsSize;
    public Vector3 BoundsCenter;

    [HideInInspector]
    public bool Unused; // when recreate, this mark object that were changed or not used due to game update or oopsies

    [HideInInspector] public bool Ignored;
}
