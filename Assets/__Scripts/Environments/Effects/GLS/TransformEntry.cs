using System;
using Beatmap.Enums;
using UnityEngine;

[Serializable]
public struct TransformEntry
{
    public int ID;
    public Transform Transform;
    public Axis Axis;
    public bool Mirrored;
}
