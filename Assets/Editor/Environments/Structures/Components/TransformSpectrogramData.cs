using System;
using System.Linq;
using Beatmap.Enums;
using UnityEngine;

public class TransformSpectrogramData : EnvironmentComponentData<TransformSpectrogram>
{
    public string[] Transforms;
    public string Axis;
    public float MinPosition;
    public float MaxPosition;
    public bool ScaleSamples = true;
    public float Scale = 1f;

    public override void SearchAndFillComponents(GameObject self, TransformSpectrogram comp, CreateContainer container)
    {
        comp.Transforms =
            Transforms
                .Select(o => container.GetGameObjectOrNull(o, self).transform)
                .ToArray();
    }

    public override void CopyTo(TransformSpectrogram comp)
    {
        comp.Axis = Enum.Parse<Axis>(Axis);
        comp.MinPosition = MinPosition;
        comp.MaxPosition = MaxPosition;
        comp.ScaleSamples = ScaleSamples;
        comp.Scale = Scale;
    }
}
