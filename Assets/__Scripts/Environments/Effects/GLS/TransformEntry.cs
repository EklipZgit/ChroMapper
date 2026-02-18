using System;
using System.Collections.Generic;
using Beatmap.Enums;
using UnityEngine;

[Serializable]
public struct TransformEntry
{
    public int ID;
    public List<Transform> Transforms;
    public Axis Axis;
    public bool Mirrored;
}
