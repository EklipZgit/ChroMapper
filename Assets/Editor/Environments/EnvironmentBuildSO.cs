using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class EnvironmentBuildSO : ScriptableObject
{
    public List<EnvironmentEntry<Material>> materials = new();
    public List<EnvironmentEntry<Mesh>> meshes = new();

    public Dictionary<string, Mesh> meshLookup = new();
    public Dictionary<string, Material> materialLookup = new();

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    private void Initialize()
    {
        meshLookup.Clear();
        foreach (var entry in meshes) meshLookup[entry.Name] = entry.Value;

        materialLookup.Clear();
        foreach (var entry in materials) materialLookup[entry.Name] = entry.Value;
    }

    public void MarkForChange()
    {
        materials.ForEach(x => x.Unused = true);
        meshes.ForEach(x => x.Unused = true);
    }

    public void RemoveUnused()
    {
        materials = materials.Where(x => !x.Unused).ToList();
        meshes = meshes.Where(x => !x.Unused).ToList();
    }

    public void AddMaterialEntry(string n) => AddEntry(materials, n);
    public void AddMeshEntry(string n) => AddEntry(meshes, n);

    private static void AddEntry<T>(List<EnvironmentEntry<T>> list, string name) where T : Object
    {
        for (var index = 0; index < list.Count; index++)
        {
            var entry = list[index];
            if (entry.Name != name) continue;

            entry.Unused = false;
            list[index] = entry;
        }

        if (list.All(x => x.Name != name)) list.Add(new EnvironmentEntry<T> { Name = name });
    }

    public void Sort()
    {
        meshes.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
        materials.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
    }
}

[Serializable]
public class EnvironmentEntry<TValue> where TValue : Object
{
    public string Name;
    public TValue Value;

    [HideInInspector]
    public bool Unused; // when recreate, this mark object that were changed or not used due to game update or oopsies

    [HideInInspector] public bool Ignored;
}
