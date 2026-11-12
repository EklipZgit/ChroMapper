using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public class NoteReflectionMipTest
    {
        private const string ReflectionTextureGuid = "a5a44867428a12fe281e07d5aa1d5823";

        [Serializable]
        private class NativeSamples
        {
            public Sample[] samples;
        }

        [Serializable]
        private class Sample
        {
            public int face;
            public int mip;
            public float[] rgb;
        }

        [Test]
        public void ReflectionRoughnessMatchesNativeFilteredMips()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Cubemap>(
                AssetDatabase.GUIDToAssetPath(ReflectionTextureGuid));
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(512));
            Assert.That(texture.mipmapCount, Is.EqualTo(10));
            Assert.That(texture.format, Is.EqualTo(TextureFormat.DXT1));
            Assert.That(texture.isReadable, Is.False);
            foreach (var name in new[] { "Note", "Bomb" })
            {
                var noteMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    $"Assets/_Graphics/Materials/Object/{name}.mat");
                Assert.That(noteMaterial.GetTexture("_EnvironmentReflectionCube"), Is.SameAs(texture), name);
            }

            var fixture = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/Tests/Fixtures/NativeNoteReflectionSamples.json");
            var samples = JsonUtility.FromJson<NativeSamples>(fixture.text).samples;
            var directions = new[]
            {
                Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
            };
            var material = new Material(Shader.Find("Hidden/Tests/NativeCubemapSample"));
            var target = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var image = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            var previousTarget = RenderTexture.active;
            var maximumError = 0f;
            var worstSample = "";
            try
            {
                material.SetTexture("_Probe", texture);
                // Native roughness mips blur the reflection. Regenerating them from the base image keeps sharp bands.
                foreach (var sample in samples)
                {
                    material.SetVector("_Direction", directions[sample.face]);
                    material.SetFloat("_Mip", sample.mip);
                    Graphics.Blit(Texture2D.blackTexture, target, material);
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                    image.Apply();
                    var actual = image.GetPixel(0, 0);
                    for (var channel = 0; channel < 3; channel++)
                    {
                        var error = Mathf.Abs(actual[channel] - sample.rgb[channel]);
                        if (error <= maximumError)
                            continue;

                        maximumError = error;
                        worstSample = $"face {sample.face}, mip {sample.mip}, channel {channel}: " +
                            $"native {sample.rgb[channel]:F4}, CM {actual[channel]:F4}";
                    }
                }

                Assert.That(maximumError, Is.LessThan(0.005f), worstSample);
            }
            finally
            {
                RenderTexture.active = previousTarget;
                target.Release();
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
