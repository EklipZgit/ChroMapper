using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Library class for on-the-fly asset instantiation/replacement when creating environments from data.
/// If an object name matches one in the library, it will be replaced with the corresponding asset.
/// This handles entire prefabs, as well as shared instances like materials and meshes.
/// </summary>
[CreateAssetMenu(fileName = "EnvironmentLibrary", menuName = "Environment/Environment Library")]
public class EnvironmentLibrary : ScriptableObject
{
    // Special material to use for the skybox
    // Ideally this should be the bloomfog skybox material.
    [field: SerializeField]
    public Material SkyboxMaterial { get; private set; }

    // Objects in this list will be ignored entirely when creating an environment
    // (This is typically Beat Saber specific objects that ChroMapper will never use, or have different implementations for)
    [SerializeField]
    private List<string> ignoreNames = new();

    // The fallback prefab to use when no replacement is found
    [SerializeField] public GameObject fallbackPrefab;

    public bool IsIgnored(string name) => ignoreNames.Exists(it => name.Contains(it));
}
