using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Library class for on-the-fly asset instantiation/replacement when creating environments from data.
/// If an object name matches one in the library, it will be replaced with the corresponding asset.
/// This handles entire prefabs, as well as shared instances like materials and meshes.
/// </summary>
[CreateAssetMenu(fileName = "EnvironmentLibrary", menuName = "Environment/Environment Library")]
public class EnvironmentLibrarySO : ScriptableObject
{
    [SerializeField] public EnvironmentMeshSO Meshes;
    [SerializeField] public EnvironmentMaterialSO Materials;
    [SerializeField] public EnvironmentSpriteSO Sprites;
    
    [SerializeField] public List<ShaderEntry> Shaders;
    
    // Special material to use for the skybox
    // Ideally this should be the bloomfog skybox material.
    [field: SerializeField] public Material SkyboxMaterial { get; private set; }
    
    [field: SerializeField] public Mesh SliceSprite { get; private set; }

    [SerializeField] public List<LayerMaskEntry> layerMaskRemap = new();
    public Dictionary<string, LayerMask> layerMaskLookup = new();

    // The fallback prefab to use when no replacement is found
    [SerializeField] public GameObject fallbackPrefab;

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    private void Initialize()
    {
        layerMaskLookup.Clear();
        foreach (var entry in layerMaskRemap) layerMaskLookup.Add(entry.name, entry.layerMask);
    }
}

[Serializable]
public struct LayerMaskEntry
{
    public string name;
    public LayerMask layerMask;
}

[Serializable]
public struct ShaderEntry
{
    public string name;
    public Shader shader;
}
