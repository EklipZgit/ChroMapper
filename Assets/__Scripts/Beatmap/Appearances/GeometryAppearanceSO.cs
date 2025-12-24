using System;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/Geometry Appearance SO", fileName = "GeometryAppearanceSO")]
    public class GeometryAppearanceSO : ScriptableObject
    {
        [SerializeField] private Material lightOpaqueMaterial;
        [SerializeField] private Material lightTransparentMaterial;
        [SerializeField] private Material shinyMaterial;
        [SerializeField] private Material obstacleMaterial;
        [SerializeField] private Material regularMaterial;

        private static BaseMaterial standard;

        public void OnEnable() => standard = new BaseMaterial{ Shader = "Standard" };

        public void SetGeometryAppearance(GeometryContainer container)
        {
            var eh = container.EnvironmentEnhancement;

            // Bail if not geometry - environment enhancement is handled elsewhere
            if (eh.Geometry == null) return;

            var descriptor = container.Context.Descriptor;
            BaseMaterial basemat = standard;
            switch (eh.Geometry[eh.GeometryKeyMaterial])
            {
                case JSONString str:
                    if (str.Value != "standard")
                    {
                        if (!BeatSaberSongContainer.Instance.Map.Materials.TryGetValue(str.Value, out basemat))
                        {
                            Debug.LogError($"Missing material \"{str.Value}\"!");
                            basemat = standard;
                        }
                    }
                    break;
                case JSONObject obj:
                    basemat = new BaseMaterial(obj);
                    break;
                default:
                    Debug.LogError("Geometry with invalid material!");
                    break;
            }

            ShaderType shader = ShaderType.Standard;
            if (!Enum.TryParse(basemat.Shader ?? "Standard", out shader))
            {
                Debug.LogError($"Invalid shader '{basemat.Shader}'!");
            }

            var meshRenderer = container.Shape.GetComponent<MeshRenderer>();

            var material = shader switch
            {
                ShaderType.OpaqueLight  => lightOpaqueMaterial,
                ShaderType.TransparentLight => lightTransparentMaterial,
                ShaderType.BaseWater => shinyMaterial,
                ShaderType.BillieWater => shinyMaterial,
                ShaderType.WaterfallMirror => shinyMaterial,
                ShaderType.Obstacle => obstacleMaterial,
                _ => regularMaterial,
            };

            var colorKeyword = Shader.PropertyToID("_Color");

            if (basemat.Color is Color color)
            {
                container.MaterialPropertyBlock.SetColor(colorKeyword, color);
            }

            // For animating material color
            if (basemat.Track is string track)
            {
                container.MaterialAnimator.AttachToMaterial(container, track);
            }

            meshRenderer.sharedMaterial = material;
            meshRenderer.SetPropertyBlock(container.MaterialPropertyBlock);

            if (eh.Components?.HasKey("ILightWithId") ?? false)
            {
                var light = container.Shape.AddComponent<LightController>();
                descriptor.BasicEventEffectManager.Register(eh.LightType ?? 0, eh.LightID ?? -1, light);
            }
        }

        // Straight outta heck
        enum ShaderType
        {
            Standard,
            OpaqueLight,
            TransparentLight,
            BaseWater,
            BillieWater,
            BTSPillar,
            InterscopeConcrete,
            InterscopeCar,
            Obstacle,
            WaterfallMirror
        }

        static bool IsLightType(ShaderType shaderType)
        {
            return shaderType == ShaderType.OpaqueLight
                || shaderType == ShaderType.TransparentLight
                || shaderType == ShaderType.BillieWater;
        }
    }
}
