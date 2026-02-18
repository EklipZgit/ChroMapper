using UnityEngine;

public class MoveInDirectionEffectComponent
{
    public string Transform;
    public Vector3 MoveOrigin;
    public float MoveScale = 1f;
    
    public void CopyTo(MoveInDirectionFx target)
    {
        target.MoveOrigin = MoveOrigin;
        target.MoveScale = MoveScale;
    }
}
