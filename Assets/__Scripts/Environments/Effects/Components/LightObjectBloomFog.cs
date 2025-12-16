using UnityEngine;

public class LightObjectBloomFog : LightObject
{
    protected override Color ModifyColor(Color color) => color * Multiply;
}
