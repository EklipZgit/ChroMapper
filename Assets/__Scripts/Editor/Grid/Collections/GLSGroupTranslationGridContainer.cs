using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class GLSGroupTranslationGridContainer : GLSGroupGridContainer<BaseLightTranslationEventBoxGroup>
{
    public override ObjectType ContainerType => ObjectType.GLSTranslation;
}
