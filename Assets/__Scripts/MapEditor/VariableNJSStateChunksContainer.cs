using System;
using Beatmap.Base;

public class VariableNJSStateChunksContainer : StateChunksContainer<VariableNJSStateData, BaseNJSEvent>
{
}

public class VariableNJSStateData : StateData<BaseNJSEvent>
{
    public Func<float, float> Easing = global::Easing.Linear;
    public float RelativeNjs;
    public float NextRelativeNjs;

    public VariableNJSStateData(BaseNJSEvent @base) : base(@base)
    {
    }
}
