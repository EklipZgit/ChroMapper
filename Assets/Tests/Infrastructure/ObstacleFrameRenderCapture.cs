using UnityEngine;

namespace Tests.Infrastructure
{
    internal static class ObstacleFrameRenderCapture
    {
        internal static Color[] Capture(MeshRenderer source, string path)
        {
            var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cameraObject = new GameObject("Obstacle frame color repro");
            var camera = cameraObject.AddComponent<Camera>();
            var material = new Material(source.sharedMaterial);
            var shaderSetup = Object.FindAnyObjectByType<PerCameraShaderSetupController>();
            var setupEnabled = shaderSetup.enabled;
            var postBloom = Shader.IsKeywordEnabled("POST_BLOOM");
            var fog = Shader.IsKeywordEnabled("BLOOM_FOG");
            var aces = Shader.IsKeywordEnabled("ACES_TONE_MAPPING");
            var boost = Shader.GetGlobalFloat("_BaseColorBoost");
            var threshold = Shader.GetGlobalFloat("_BaseColorBoostThreshold");
            try
            {
                // Keep the real material, color, and cutout inputs while making even distant water edges sampleable.
                var renderer = model.GetComponent<MeshRenderer>();
                // Isolate signed alpha from the two environments' different height-fog ramps.
                material.SetFloat("_FogHeightScale", 0f);
                material.SetFloat("_FogHeightOffset", Shader.GetGlobalFloat("_CustomFogHeightFogStartY")
                    + 2f * Shader.GetGlobalFloat("_CustomFogHeightFogHeight"));
                renderer.sharedMaterial = material;
                renderer.enabled = source.enabled;
                var properties = new MaterialPropertyBlock();
                source.GetPropertyBlock(properties);
                properties.SetVector("_WorldScale", Vector3.one);
                properties.SetVector("_SizeParams", new Vector4(0.5f, 0.5f, 0.5f, 0.025f));
                renderer.SetPropertyBlock(properties);
                model.layer = 30;
                camera.enabled = false;
                camera.cullingMask = 1 << 30;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.orthographicSize = 0.8f;
                camera.nearClipPlane = 0.01f;
                camera.transform.SetPositionAndRotation(Vector3.back * 3f, Quaternion.identity);
                // Native MAIN_EFFECT_ENABLED preserves frame RGB and sends signed alpha to the post-process.
                shaderSetup.enabled = false;
                Shader.EnableKeyword("POST_BLOOM");
                Shader.DisableKeyword("BLOOM_FOG");
                return LinearRenderCapture.Capture(camera, path, 256, 256);
            }
            finally
            {
                if (!postBloom)
                    Shader.DisableKeyword("POST_BLOOM");
                if (fog)
                    Shader.EnableKeyword("BLOOM_FOG");
                if (aces)
                    Shader.EnableKeyword("ACES_TONE_MAPPING");
                else
                    Shader.DisableKeyword("ACES_TONE_MAPPING");
                Shader.SetGlobalFloat("_BaseColorBoost", boost);
                Shader.SetGlobalFloat("_BaseColorBoostThreshold", threshold);
                shaderSetup.enabled = setupEnabled;
                Object.DestroyImmediate(model);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(material);
            }
        }
    }
}
