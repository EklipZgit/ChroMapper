using UnityEngine;

public class AlphaFloatFxGroupEffectTargetComponent
{
    public string[] MaterialPropertyBlockControllers;
    public string Property;
    public float[] StaticColor;

    public void CopyTo(AlphaFx target)
    {
        target.Property = Property;
        target.StaticColor = new Color(StaticColor[0], StaticColor[1], StaticColor[2], StaticColor[3]);
    }
}
