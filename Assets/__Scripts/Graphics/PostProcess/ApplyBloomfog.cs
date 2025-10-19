using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(ApplyBloomfogRenderer), PostProcessEvent.AfterStack, "ChroMapper/Apply Bloomfog")]
public sealed class ApplyBloomfog : PostProcessEffectSettings
{
}

public sealed class ApplyBloomfogRenderer : PostProcessEffectRenderer<ApplyBloomfog>
{
    private static readonly Shader shader = Shader.Find("ChroMapper/Post Process/ApplyBloomfog");

    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(shader);
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}
