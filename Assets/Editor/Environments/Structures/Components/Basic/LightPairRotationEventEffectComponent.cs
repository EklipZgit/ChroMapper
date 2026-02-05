using UnityEngine;

public class LightPairRotationEventEffectComponent
{
    public bool IsEnabled;
    
    public string EventTypeL;
    public string TransformL;
    public string EventTypeR;
    public string TransformR;
    public string SwitchOverrideRandomValuesEvent;
    public Vector3 RotationVector;
    public bool OverrideRandomValues;
    public bool UseZPositionForAngleOffset;
    public float ZPositionAngleOffsetScale;
    public float StartRotation;
}
