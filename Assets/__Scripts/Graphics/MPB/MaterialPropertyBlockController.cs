using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MaterialPropertyBlockController : MonoBehaviour
{
    public Renderer[] Renderers = Array.Empty<Renderer>();
    public MaterialPropertyBlock Mpb => mpb ??= new MaterialPropertyBlock();
    private MaterialPropertyBlock mpb;

    private void Start() => mpb ??= new MaterialPropertyBlock();

    public void ApplyChanges()
    {
        for (var i = 0; i < Renderers.Length; i++) Renderers[i].SetPropertyBlock(mpb);
    }

    public void ShowRenderer(bool active)
    {
        for (var i = 0; i < Renderers.Length; i++) Renderers[i].enabled = active;
    }

    // TODO: this is genuinely painful but i have to do it this way or break serial
    public void Add(Renderer r) => Renderers = Renderers.Append(r).ToArray();
    public void Add(IEnumerable<Renderer> r) => Renderers = Renderers.Concat(r).ToArray();

    public void Remove(Renderer r)
    {
        var list = Renderers.ToList();
        list.Remove(r);
        Renderers = list.ToArray();
    }

    public void Remove(IEnumerable<Renderer> r)
    {
        var list = Renderers.ToList();
        foreach (var u in r) list.Remove(u);
        Renderers = list.ToArray();
    }
}
