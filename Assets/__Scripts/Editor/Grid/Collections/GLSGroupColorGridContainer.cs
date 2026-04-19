using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class GLSGroupColorGridContainer : GLSGroupGridContainer<BaseLightColorEventBoxGroup>
{
    public override ObjectType ContainerType => ObjectType.GLSColor;
}
