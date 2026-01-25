using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Environment/Environment List", fileName = "EnvironmentListSO")]
public class EnvironmentListSO : ScriptableObject
{
    [SerializeField] public List<EnvironmentListInfo> List = new();

    private readonly Dictionary<string, EnvironmentListInfo> lookupID = new();
    private readonly string defaultEnvironment = "DefaultEnvironment";

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    private void Initialize()
    {
        lookupID.Clear();
        foreach (var entry in List) lookupID[entry.ID] = entry;
    }

    public EnvironmentListInfo GetEnvironmentOrDefault(string environment) =>
        lookupID.TryGetValue(environment, out var env) && !env.Ignore ? env : lookupID[defaultEnvironment];
}

[Serializable]
public class EnvironmentListInfo
{
    public string Name;
    public string ID;
    public TracksDefinitionSO TracksDefinition;
    public ColorSchemeSO ColorScheme;
    public bool Ignore;
}
