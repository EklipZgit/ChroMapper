using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Environment/Environment Material", fileName = "EnvironmentMaterialSO")]
public class EnvironmentMaterialSO : ScriptableObject
{
    [SerializeField] public List<MaterialInfo> list = new();

    public readonly Dictionary<string, Material> Lookup = new();

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    private void Initialize()
    {
        Lookup.Clear();
        foreach (var entry in list) Lookup[entry.Hash] = entry.Material;
    }

    public void MarkForChange()
    {
        list.ForEach(x =>
        {
            x.Unused = true;
            x.Environments.Clear();
        });
    }

    public void RemoveUnused() => list.RemoveAll(x => x.Unused);

    public void AddEntry(EnvInfoMaterial material, string environment)
    {
        for (var index = 0; index < list.Count; index++)
        {
            var entry = list[index];
            if (entry.Hash != material.Hash) continue;

            entry.Unused = false;
            list[index] = entry;
        }

        if (list.All(x => x.Hash != material.Hash))
        {
            list.Add(
                new MaterialInfo
                {
                    Hash = material.Hash,
                    Name = material.Name,
                    Shader = material.Shader,
                    Color = GetColor(material.Color),
                    Environments = new List<string> { environment }
                });
        }
        else
        {
            var m = list.First(x => x.Hash == material.Hash);
            m.Color = GetColor(material.Color);
            if (!m.Environments.Contains(environment)) m.Environments.Add(environment);
        }
    }

    private Color GetColor(float[] val) =>
        Mathf.Approximately(val[0], -1) ? new Color(0f, 0.5f, 1f) : new Color(val[0], val[1], val[2], val[3]);

    public void Sort() => list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
}

[Serializable]
public class MaterialInfo
{
    public Material Material;
    public string Hash;
    public string Name;
    public string Shader;

    public Color Color;

    public List<string> Keywords;
    public List<string> Environments;

    [HideInInspector]
    public bool Unused; // when recreate, this mark object that were changed or not used due to game update or oopsies

    [HideInInspector] public bool Ignored;
}
