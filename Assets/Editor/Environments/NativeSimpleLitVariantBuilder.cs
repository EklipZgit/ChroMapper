using System.Linq;
using Beatmap.Appearances;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class NativeSimpleLitVariantBuilder : IPreprocessBuildWithReport
{
    private const string KeywordsPath = "Assets/_Graphics/Shaders/ShaderLibrary/NativeSimpleLitVariants.json";
    public const string VariantsPath = "Assets/_Graphics/Shaders/ShaderLibrary/NativeSimpleLitVariants.asset";
    private const string AppearancePath = "Assets/__Scripts/Beatmap/Appearances/GeometryAppearanceSO.asset";
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report) => UpdateVariants();

    [MenuItem("Environment/Update Native SimpleLit Variants")]
    public static void UpdateVariants()
    {
        var keywords = AssetDatabase.LoadAssetAtPath<TextAsset>(KeywordsPath);
        var catalog = new NativeShaderVariants(keywords.text);
        var variants = AssetDatabase.LoadAssetAtPath<NativeShaderVariantSeeds>(VariantsPath);
        if (variants == null)
        {
            variants = ScriptableObject.CreateInstance<NativeShaderVariantSeeds>();
            AssetDatabase.CreateAsset(variants, VariantsPath);
        }
        var shader = Shader.Find("ChroMapper/Lit");
        var nativeVariants = catalog.GetVariants();
        var materials = AssetDatabase.LoadAllAssetsAtPath(VariantsPath).OfType<Material>().ToList();
        while (materials.Count < nativeVariants.Length)
        {
            var material = new Material(shader) { name = "Native SimpleLit " + materials.Count };
            AssetDatabase.AddObjectToAsset(material, variants);
            materials.Add(material);
        }
        for (var index = 0; index < nativeVariants.Length; index++)
        {
            var portedKeywords = nativeVariants[index]
                .Select(keyword => GeometryAppearanceSO.CanonicalizeKeyword(keyword, GeometryAppearanceSO.ShaderType.Standard))
                .Where(keyword => shader.keywordSpace.FindKeyword(keyword).isValid)
                .Distinct().ToArray();
            materials[index].shader = shader;
            materials[index].shaderKeywords = portedKeywords;
            materials[index].enableInstancing = true;
            EditorUtility.SetDirty(materials[index]);
        }
        for (var index = materials.Count - 1; index >= nativeVariants.Length; index--)
        {
            Object.DestroyImmediate(materials[index], true);
            materials.RemoveAt(index);
        }
        variants.Materials = materials.ToArray();
        EditorUtility.SetDirty(variants);
        AssetDatabase.SaveAssetIfDirty(variants);

        // The appearance asset retains this collection in player builds, including the empty native fallback.
        var appearance = AssetDatabase.LoadAssetAtPath<GeometryAppearanceSO>(AppearancePath);
        var serialized = new SerializedObject(appearance);
        serialized.FindProperty("nativeSimpleLitKeywords").objectReferenceValue = keywords;
        serialized.FindProperty("nativeSimpleLitVariants").objectReferenceValue = variants;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(appearance);
    }
}
