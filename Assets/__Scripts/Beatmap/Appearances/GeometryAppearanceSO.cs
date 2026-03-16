using System;
using System.Linq;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/Geometry Appearance SO", fileName = "GeometryAppearanceSO")]
    public class GeometryAppearanceSO : ScriptableObject
    {
        [SerializeField] private Material regularMaterial;
        [SerializeField] private Material lightOpaqueMaterial;
        [SerializeField] private Material lightTransparentMaterial;
        [SerializeField] private Material glowingMaterial;
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Material btsMaterial;
        [SerializeField] private Material obstacleMaterial;

        private static BaseMaterial standard;
        private static readonly int colorId = Shader.PropertyToID("_Color");

        public void OnEnable() => standard = new BaseMaterial { Shader = "Standard" };

        public void SetGeometryAppearance(GeometryContainer container)
        {
            var eh = container.EnvironmentEnhancement;

            // Bail if not geometry - environment enhancement is handled elsewhere
            if (eh.Geometry == null) return;

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
                Debug.LogError($"Invalid shader '{basemat.Shader}'!");

            var material = shader switch
            {
                ShaderType.OpaqueLight => lightOpaqueMaterial,
                ShaderType.TransparentLight => lightTransparentMaterial,
                ShaderType.Glowing => glowingMaterial,
                ShaderType.BaseWater => waterMaterial,
                ShaderType.BillieWater => waterMaterial,
                ShaderType.WaterfallMirror => waterMaterial,
                ShaderType.BTSPillar => btsMaterial,
                ShaderType.Obstacle => obstacleMaterial,
                _ => regularMaterial,
            };

            if (eh.Geometry[eh.GeometryKeyMaterial].IsObject
                && eh.Geometry[eh.GeometryKeyMaterial][eh.GeometryKeyMaterialKeywords].IsArray)
            {
                if ((shader == ShaderType.Standard || shader == ShaderType.BTSPillar)
                    && eh.Geometry[eh.GeometryKeyMaterial][eh.GeometryKeyMaterialKeywords].Count == 0)
                    material = glowingMaterial;
                else
                {
                    material.shaderKeywords =
                        eh.Geometry[eh.GeometryKeyMaterial][eh.GeometryKeyMaterialKeywords]
                            .AsArray.Children.Where(x => x.IsString)
                            .Cast<string>()
                            .ToArray();
                }
            }

            if (basemat.Color is Color color) container.MpbController.Mpb.SetColor(colorId, color);

            // For animating material color
            if (basemat.Track is string track) container.MaterialAnimator.AttachToMaterial(container, track);

            foreach (var r in container.MpbController.Renderers) r.sharedMaterial = material;
            container.MpbController.ApplyChanges();
        }

        // Straight outta heck
        enum ShaderType
        {
            Standard,
            OpaqueLight,
            TransparentLight,
            Glowing,
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
