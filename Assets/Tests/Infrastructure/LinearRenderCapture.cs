using System.IO;
using UnityEngine;

namespace Tests.Infrastructure
{
    public static class LinearRenderCapture
    {
        public static Color[] Capture(Camera camera, string path, int width = 1280, int height = 720, int antiAliasing = 1)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.antiAliasing = antiAliasing;
            var image = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                var pixels = image.GetPixels();
                var displayPixels = new Color[pixels.Length];
                for (var index = 0; index < pixels.Length; index++)
                {
                    displayPixels[index] = pixels[index].gamma;
                }
                var display = new Texture2D(width, height, TextureFormat.RGB24, false);
                try
                {
                    display.SetPixels(displayPixels);
                    display.Apply();
                    File.WriteAllBytes(path, display.EncodeToPNG());
                }
                finally
                {
                    Object.DestroyImmediate(display);
                }
                return pixels;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }
    }
}
