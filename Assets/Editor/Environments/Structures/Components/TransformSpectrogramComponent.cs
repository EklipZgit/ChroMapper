using System;
using Beatmap.Enums;

public class TransformSpectrogramComponent : EnvDataComponent<TransformSpectrogram>
{
    public string[] Transforms;
    public string Axis;
    public float MinPosition;
    public float MaxPosition;
    public bool ScaleSamples = true;
    public float Scale = 1f;

    public override void CopyTo(TransformSpectrogram target)
    {
        target.Axis = Enum.Parse<Axis>(Axis);
        target.MinPosition = MinPosition;
        target.MaxPosition = MaxPosition;
        target.ScaleSamples = ScaleSamples;
        target.Scale = Scale;
    }
}
