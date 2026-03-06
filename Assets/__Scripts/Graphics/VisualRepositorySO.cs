using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "VisualRepositorySO", menuName = "Graphics/Create Visual Repository")]
public class VisualRepositorySO : ScriptableObject
{
    public List<VisualModelSO> Models;
    public List<Material> Materials;

    public Dictionary<string, VisualModelSO> ModelsByName;
    public Dictionary<string, Material> MaterialsByName;

    private readonly List<VisualModelSO> temporaryModels = new();
    private readonly List<Material> temporaryMaterials = new();

    public void OnEnable()
    {
        ModelsByName = Models.ToDictionary(x => x.name, x => x);
        MaterialsByName = Materials.ToDictionary(x => x.name, x => x);
    }

    public void OnDestroy() => Reset();

    public void Reset()
    {
        foreach (var model in temporaryModels) Models.Remove(model);
        temporaryModels.Clear();
        foreach (var material in temporaryMaterials) Materials.Remove(material);
        temporaryMaterials.Clear();
    }

    public void Add(VisualModelSO model)
    {
        Models.Add(model);
        temporaryModels.Add(model);
        ModelsByName.Add(model.name, model);
    }

    public void Add(Material material)
    {
        Materials.Add(material);
        temporaryMaterials.Add(material);
        MaterialsByName.Add(material.name, material);
    }
}
