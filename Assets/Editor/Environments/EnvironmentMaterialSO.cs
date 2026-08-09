using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Environment/Environment Material", fileName = "EnvironmentMaterialSO")]
public class EnvironmentMaterialSO : ScriptableObject
{
    [SerializeField] public List<MaterialInfo> list = new();

    public readonly Dictionary<string, Material> Lookup = new();

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    // Refresh callers need the runtime lookup immediately, without waiting for Unity to re-enable the asset.
    public void RebuildLookup() => Initialize();

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
            x.Keywords.Clear();
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
                    Keywords = new List<string>(material.Keywords),
                    FloatProps =
                        material
                            .ShaderProps.Where(x => x.Value is float)
                            .Select(x =>
                                new MaterialInfo.ShaderProps<float> { Key = x.Key, Value = x.Value })
                            .ToList(),
                    VectorProps =
                        material
                            .ShaderProps.Where(x => x.Value is not double)
                            .Select(x =>
                                new MaterialInfo.ShaderProps<Vector4>
                                {
                                    Key = x.Key,
                                    Value = ConvertUtils.ToVector4(((JArray)x.Value).ToObject<float[]>())
                                })
                            .ToList(),
                    Environments = new List<string> { environment },
                });
        }
        else
        {
            var m = list.First(x => x.Hash == material.Hash);
            m.Color = GetColor(material.Color);
            if (material.Keywords != null) m.Keywords.AddRange(material.Keywords.Where(x => !m.Keywords.Contains(x)));
            m.FloatProps.AddRange(
                material
                    .ShaderProps.Where(x => x.Value is float or double or long)
                    .Where(x => !m.FloatProps.Exists(y => y.Key == x.Key))
                    .Select(x =>
                        new MaterialInfo.ShaderProps<float> { Key = x.Key, Value = (float)x.Value }));
            m.VectorProps.AddRange(
                material
                    .ShaderProps.Where(x => x.Value is not double && x.Value is not long)
                    .Where(x => !m.VectorProps.Exists(y => y.Key == x.Key))
                    .Select(x => new MaterialInfo.ShaderProps<Vector4>
                    {
                        Key = x.Key,
                        Value =
                            ConvertUtils.ToVector4(((JArray)x.Value).ToObject<float[]>())
                    }));
            if (!m.Environments.Contains(environment)) m.Environments.Add(environment);
        }
    }

    private Color GetColor(float[] val) =>
        Mathf.Approximately(val[0], -1) ? new Color(0f, 0.5f, 1f) : new Color(val[0], val[1], val[2], val[3]);

    public void Sort()
    {
        list = list.OrderBy(x => x.Name.First()).ThenBy(x => x.Hash).ToList();
    }
}

[Serializable]
public class MaterialInfo
{
    public string Name;
    public string Hash;
    public Material Material;
    public string Shader;

    public Color Color;

    public List<string> Keywords;
    public List<ShaderProps<float>> FloatProps;
    public List<ShaderProps<Vector4>> VectorProps;
    public List<string> Environments;

    [HideInInspector]
    public bool Unused; // when recreate, this mark object that were changed or not used due to game update or oopsies

    [HideInInspector] public bool Ignored;

    [Serializable]
    public class ShaderProps<T>
    {
        public string Key;
        public T Value;
    }
}
