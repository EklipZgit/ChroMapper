using System;
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
}
