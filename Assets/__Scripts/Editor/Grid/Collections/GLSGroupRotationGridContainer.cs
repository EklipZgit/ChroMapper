using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class GLSGroupRotationGridContainer : GLSGroupGridContainer<BaseLightRotationEventBoxGroup>
{
    public override ObjectType ContainerType => ObjectType.GLSRotation;
}
