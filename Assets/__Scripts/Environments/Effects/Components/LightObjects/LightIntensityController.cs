using UnityEngine;

public class LightIntensityController : LightController
{
    public float Intensity;

    protected override bool Initialize() => true;
    public override void SetColor(Color color) => Color = color;
}
