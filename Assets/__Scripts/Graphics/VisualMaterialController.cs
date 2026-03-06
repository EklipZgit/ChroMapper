using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VisualMaterialController : VisualController
{
    public Material Default;

    public void Set(string n)
    {
        foreach (var r in MpbController.Renderers) r.sharedMaterials = Array.Empty<Material>();
        Add(n);
    }

    public void Set(Material m)
    {
        foreach (var r in MpbController.Renderers) r.sharedMaterials = Array.Empty<Material>();
        Add(m);
    }

    public void Add(string n) => Add(Repository.MaterialsByName.GetValueOrDefault(n, Default));

    public void Add(Material m)
    {
        foreach (var r in MpbController.Renderers) r.sharedMaterials = r.sharedMaterials.Append(m).ToArray();
        MpbController.ApplyChanges();
    }
}
