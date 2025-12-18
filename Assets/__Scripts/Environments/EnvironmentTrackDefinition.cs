using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class EnvironmentTrackDefinition
{
    [SerializeField] private List<TrackDefinitionBasic> BasicEntries = new();
    [SerializeField] private List<TrackDefinitionGLS> GlsEntries = new();

    [NonSerialized] public Dictionary<int, TrackDefinitionBasic> Basic;
    [NonSerialized] public Dictionary<int, TrackDefinitionGLS> Gls;

    public void Initialize()
    {
        Basic = BasicEntries.ToDictionary(x => x.Type, x => x);
        Gls = GlsEntries.Select((x, i) => (i, x)).ToDictionary(x => x.i, x => x.x);
    }

    public void Register(TrackDefinitionBasic basic) => BasicEntries.Add(basic);
    public void Register(TrackDefinitionGLS gls) => GlsEntries.Add(gls);
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
