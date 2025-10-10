using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(CustomTonemappingRenderer), PostProcessEvent.AfterStack, "ChroMapper/Tonemapping")]
public sealed class CustomTonemapping : PostProcessEffectSettings
{
}

public sealed class CustomTonemappingRenderer : PostProcessEffectRenderer<CustomTonemapping>
{
   private static readonly Shader shader = Shader.Find("ChroMapper/Post Process/Tonemapping");

   public override void Render(PostProcessRenderContext context)
   {
      var sheet = context.propertySheets.Get(shader);
      context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
   }
}