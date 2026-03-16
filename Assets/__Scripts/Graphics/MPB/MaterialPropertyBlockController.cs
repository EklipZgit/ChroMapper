using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MaterialPropertyBlockController : MonoBehaviour
{
    public List<Renderer> Renderers = new();
    private readonly Dictionary<Renderer, int> indexMap = new();

    public MaterialPropertyBlock Mpb => mpb ??= new MaterialPropertyBlock();
    private MaterialPropertyBlock mpb;

    private void Start()
    {
        mpb ??= new MaterialPropertyBlock();
        for (var i = 0; i < Renderers.Count; i++) indexMap[Renderers[i]] = i;
    }

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

    public void Add(Renderer r)
    {
        Renderers.Add(r);
        indexMap[r] = Renderers.Count - 1;
    }

    public void Add(IEnumerable<Renderer> list)
    {
        foreach (var r in list) Add(r);
    }

    public void Remove(Renderer r)
    {
        if (indexMap.TryGetValue(r, out var v))
        {
            var lastRenderer = Renderers[^1];
            Renderers.RemoveAtSwapBack(v);
            if (lastRenderer != r) indexMap[lastRenderer] = indexMap[r];
            indexMap.Remove(r);
        }
        else
            Renderers.Remove(r);
    }

    public void Remove(IEnumerable<Renderer> r)
    {
        foreach (var u in r) Remove(u);
    }
}
