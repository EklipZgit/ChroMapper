using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class EnvironmentTrackDefinition
{
    [SerializeField] public List<TrackDefinitionBasic> BasicEntries;
    [SerializeField] public List<TrackDefinitionGLS> GlsEntries;

    [NonSerialized] public TrackDefinitionBasic[] Basic;
    [NonSerialized] public Dictionary<string, TrackDefinitionGLS> Gls;

    public void Initialize()
    {
        Basic = BasicEntries.ToArray();
        Gls = GlsEntries.ToDictionary(x => x.Group, x => x);
    }
}

[Serializable]
public class TrackDefinitionBasic
{
    public string Name;
    public int Type;
}

[Serializable]
public class TrackDefinitionGLS
{
    public string Group;
    public string Name;

    public bool ColorTrack;

    public bool[] RotationTracks = new bool[3];
    public string OverrideDefaultRotationAxis;

    public bool[] TranslationTracks = new bool[3];
    public string OverrideDefaultTranslationAxis;

    public bool FloatFXTrack;

    public bool Duplicate;
}
