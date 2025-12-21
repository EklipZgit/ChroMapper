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
    [SerializeField] public List<EnvironmentListInfo> list = new();

    public readonly Dictionary<string, EnvironmentListInfo> LookupID = new();
    public readonly string DefaultEnvironment = "DefaultEnvironment";

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    private void Initialize()
    {
        LookupID.Clear();
        foreach (var entry in list) LookupID[entry.ID] = entry;
    }

    public EnvironmentListInfo GetEnvironmentOrDefault(string environment) =>
        LookupID.TryGetValue(environment, out var env) ? env : LookupID[DefaultEnvironment];
}

[Serializable]
public class EnvironmentListInfo
{
    public string Name;
    public string ID;
    public TracksDefinitionSO TracksDefinition;
    public ColorSchemeSO ColorScheme;
}
