using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MaterialPropertyBlockController : MonoBehaviour
{
    public List<Renderer> Renderers = new();
    public MaterialPropertyBlock Mpb => mpb ??= new MaterialPropertyBlock();
    private MaterialPropertyBlock mpb;

    private void Start() => mpb ??= new MaterialPropertyBlock();

    public void ApplyChanges()
    {
        var len = Renderers.Count;
        for (var i = 0; i < len; i++) Renderers[i].SetPropertyBlock(mpb);
    }

    public void ShowRenderer(bool active)
    {
        var len = Renderers.Count;
        for (var i = 0; i < len; i++) Renderers[i].enabled = active;
    }

    public void Add(Renderer r) => Renderers.Add(r);
    public void Add(IEnumerable<Renderer> r) => Renderers.AddRange(r);

    public void Remove(Renderer r) => Renderers.Remove(r);

    public void Remove(IEnumerable<Renderer> r)
    {
        foreach (var u in r) Renderers.Remove(u);
    }
}
