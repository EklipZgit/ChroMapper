using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using FloatParameter = UnityEngine.Rendering.PostProcessing.FloatParameter;

// Modified Unity's Post Processing bloom effect to match Beat Saber bloom behaviour
[Serializable]
[PostProcess(typeof(CustomBloomRenderer), PostProcessEvent.BeforeStack, "ChroMapper/Bloom")]
public sealed class CustomBloom : PostProcessEffectSettings
{
   [Range(0f, 10f),
    Tooltip(
       "Strength of the bloom filter. Values higher than 1 will make bloom contribute more energy to the final render.")]
   public FloatParameter intensity = new() { value = 0f };

   // [Range(0f, 4f),
   //  Tooltip("Filters out pixels under this level of brightness. Value is in gamma-space.")]
   // public FloatParameter threshold = new() { value = 1f };
   //
   // [Range(0f, 1f),
   //  Tooltip("Makes transitions between under/over-threshold gradual. 0 for a hard threshold, 1 for a soft threshold).")]
   // public FloatParameter softKnee = new() { value = 0.5f };
   //
   // [Tooltip("Clamps pixels to control the bloom amount. Value is in gamma-space.")]
   // public FloatParameter clamp = new() { value = 65472f };

   [Range(1f, 10f),
    Tooltip(
       "Changes the extent of veiling effects. For maximum quality, use integer values. Because this value changes the internal iteration count, You should not animating it as it may introduce issues with the perceived radius.")]
   public FloatParameter diffusion = new() { value = 7f };

   // [ColorUsage(false, true), Tooltip("Global tint of the bloom filter.")]
   // public ColorParameter color = new() { value = Color.white };

   [Tooltip(
      "Boost performance by lowering the effect quality. This settings is meant to be used on mobile and other low-end platforms but can also provide a nice performance boost on desktops and consoles.")]
   public BoolParameter fastMode = new() { value = false };

   public BloomResolutionParameter resolution = new() { value = BloomResolution.Full };

   public override bool IsEnabledAndSupported(PostProcessRenderContext context)
   {
      return enabled.value
         && intensity.value > 0f;
   }
}

public sealed class CustomBloomRenderer : PostProcessEffectRenderer<CustomBloom>
{
   private static readonly Shader shaderBloom = Shader.Find("ChroMapper/Post Process/Bloom");
   private static readonly int shaderIdSampleScale = Shader.PropertyToID("_SampleScale");
   private static readonly int shaderIdIntensity = Shader.PropertyToID("_Intensity");
   private static readonly int shaderIdThreshold = Shader.PropertyToID("_Threshold");
   private static readonly int shaderIdParams = Shader.PropertyToID("_Params");
   private static readonly int shaderIdBloomTex = Shader.PropertyToID("_BloomTex");

   enum Pass
   {
      Prefilter,
      Downsample13,
      Downsample4,
      UpsampleTent,
      UpsampleBox,
      Composite
   }

   private Level[] _pyramid;
   private const int MaxPyramidSize = 16;

   private struct Level
   {
      public int Down;
      public int Up;
   }

   public override void Init()
   {
      _pyramid = new Level[MaxPyramidSize];

      for (var i = 0; i < MaxPyramidSize; i++)
      {
         _pyramid[i] = new Level
         {
            Down = Shader.PropertyToID("_BloomMipDown" + i),
            Up = Shader.PropertyToID("_BloomMipUp" + i)
         };
      }
   }

   public override void Render(PostProcessRenderContext context)
   {
      var cmd = context.command;
      cmd.BeginSample("Alpha Bloom");
      var sheet = context.propertySheets.Get(shaderBloom);

      // Apply auto exposure adjustment in the prefiltering pass
      // sheet.properties.SetTexture(ShaderIDs.AutoExposureTex, context.autoExposureTexture);

      // fillrate limited platforms
      // Full res bloom because I can
      var tw = context.screenWidth;
      var th = context.screenHeight;
      if (settings.resolution.value == BloomResolution.Half)
      {
         tw = Mathf.FloorToInt(tw / 2f);
         th = Mathf.FloorToInt(th / 2f);
      }

      var singlePassDoubleWide = context.stereoActive
         && context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass
         && context.camera.stereoTargetEye == StereoTargetEyeMask.Both;
      var twStereo = singlePassDoubleWide ? tw * 2 : tw;

      // Determine the iteration count
      var s = Mathf.Max(tw, th);
      var logs = Mathf.Log(s, 2f) + Mathf.Min(settings.diffusion.value, 10f) - 10f;
      var logsI = Mathf.FloorToInt(logs);
      var iterations = Mathf.Clamp(logsI, 1, MaxPyramidSize);
      var sampleScale = 0.5f + logs - logsI;
      sheet.properties.SetFloat(shaderIdSampleScale, sampleScale);

      // We don't use luminousity, but possible we could use it
      // Prefiltering parameters
      // var lthresh = Mathf.GammaToLinearSpace(settings.threshold.value);
      // var knee = lthresh * settings.softKnee.value + 1e-5f;
      // var threshold = new Vector4(lthresh, lthresh - knee, knee * 2f, 0.25f / knee);
      // sheet.properties.SetVector(shaderIdThreshold, threshold);
      // var lclamp = Mathf.GammaToLinearSpace(settings.clamp.value);
      // sheet.properties.SetVector(shaderIdParams, new Vector4(lclamp, 0f, 0f, 0f));

      var qualityOffset = settings.fastMode ? 1 : 0;

      // Downsample
      var lastDown = context.source;
      for (var i = 0; i < iterations; i++)
      {
         var mipDown = _pyramid[i].Down;
         var mipUp = _pyramid[i].Up;
         var pass = i == 0 ? (int)Pass.Prefilter : (int)Pass.Downsample13 + qualityOffset;

         context.GetScreenSpaceTemporaryRT(
            cmd,
            mipDown,
            0,
            context.sourceFormat,
            RenderTextureReadWrite.Default,
            FilterMode.Bilinear,
            twStereo,
            th);
         context.GetScreenSpaceTemporaryRT(
            cmd,
            mipUp,
            0,
            context.sourceFormat,
            RenderTextureReadWrite.Default,
            FilterMode.Bilinear,
            twStereo,
            th);
         cmd.BlitFullscreenTriangle(lastDown, mipDown, sheet, pass);

         lastDown = mipDown;
         twStereo = singlePassDoubleWide && twStereo / 2 % 2 > 0 ? 1 + twStereo / 2 : twStereo / 2;
         twStereo = Mathf.Max(twStereo, 1);
         th = Mathf.Max(th / 2, 1);
      }

      // Upsample
      var lastUp = _pyramid[iterations - 1].Down;
      for (var i = iterations - 2; i >= 0; i--)
      {
         var mipDown = _pyramid[i].Down;
         var mipUp = _pyramid[i].Up;
         cmd.SetGlobalTexture(shaderIdBloomTex, mipDown);
         cmd.BlitFullscreenTriangle(lastUp, mipUp, sheet, (int)Pass.UpsampleTent + qualityOffset);
         lastUp = mipUp;
      }

      // Composite
      var intensity = RuntimeUtilities.Exp2(settings.intensity.value / 10f) - 1f;
      sheet.properties.SetFloat(shaderIdIntensity, intensity);
      cmd.SetGlobalTexture(shaderIdBloomTex, lastUp);
      cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.Composite);

      // Debug
      // cmd.BlitFullscreenTriangle(_pyramid[0].Down, context.destination, sheet, 6);
      // cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 6);
      // cmd.BlitFullscreenTriangle(_pyramid[0].Up, context.destination, sheet, 6);

      // Cleanup
      for (var i = 0; i < iterations; i++)
      {
         if (_pyramid[i].Down != lastUp) cmd.ReleaseTemporaryRT(_pyramid[i].Down);
         if (_pyramid[i].Up != lastUp) cmd.ReleaseTemporaryRT(_pyramid[i].Up);
      }

      cmd.EndSample("Alpha Bloom");
   }
}

// Quarter or Triplet size is actually pretty bad
public enum BloomResolution
{
   Full,
   Half
}

[Serializable]
public sealed class BloomResolutionParameter : ParameterOverride<BloomResolution>
{
}