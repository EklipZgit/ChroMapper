using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Library class for on-the-fly asset instantiation/replacement when creating environments from data.
/// If an object name matches one in the library, it will be replaced with the corresponding asset.
/// This handles entire prefabs, as well as shared instances like materials and meshes.
/// </summary>
[CreateAssetMenu(fileName = "EnvironmentLibrary", menuName = "Environment/Environment Library")]
public class EnvironmentLibrary : ScriptableObject
{
    // Objects in this list will be ignored entirely when creating an environment
    // (This is typically Beat Saber specific objects that ChroMapper will never use, or have different implementations for)
    [SerializeField]
    private List<string> ignoreNames = new();

    // Main list of replacements
    [SerializeField]
    private List<LibraryEntry> library = new();

    // Internal map for quick lookup (because Unity cannot serialize Dictionaries)
    private Dictionary<string, Object> replacementMap = new();

    // The fallback prefab to use when no replacement is found
    [SerializeField]
    private GameObject fallbackPrefab;

    private void OnValidate() => Initialize();

    private void OnEnable() => Initialize();

    public void Initialize()
    {
        replacementMap.Clear();

        foreach (var entry in library)
        {
            replacementMap.Add(entry.Name, entry.Replacement);
        }
    }

    public bool IsIgnored(string name) => ignoreNames.Exists(it => name.Contains(it));

    public bool HasReplacement(string name) => replacementMap.ContainsKey(name);

    // Retrieves a shared instance of any environment object (Material, Mesh, etc.)
    public Object RetrieveEnvironmentObject(string name)
        => replacementMap.TryGetValue(name, out var prefab)
            ? prefab
            : fallbackPrefab;

    // Instantiates a new instance of an environment object (Prefabs, mostly)
    public Object InstantiateEnvironmentObject(string name)
        => replacementMap.TryGetValue(name, out var prefab)
            ? Instantiate(prefab)
            : Instantiate(fallbackPrefab);

    // Unity cannot serialize a Dictionary, so we use a List of entries instead
    [System.Serializable]
    public class LibraryEntry
    {
        public string Name;
        public Object Replacement;
    }
}
