using UnityEngine;

public class TrackLaneRingsRotationEffectData : EnvironmentComponentData<TrackLaneRingsRotation>
{
    public string TrackLaneRingsManager;
    public float StartupRotationAngle;
    public float StartupRotationStep;
    public int StartupRotationPropagationSpeed;
    public float StartupRotationFlexySpeed;

    public override void SearchAndFillComponents(GameObject self, TrackLaneRingsRotation comp, CreateContainer container)
    {
        comp.Manager = container
            .GetGameObjectOrNull(TrackLaneRingsManager, self)
            .GetComponent<TrackLaneRingsManager>();
    }

    public override void CopyTo(TrackLaneRingsRotation comp)
    {
        comp.StartupRotationAngle = StartupRotationAngle;
        comp.StartupRotationStep = StartupRotationStep;
        comp.StartupRotationPropagationSpeed = StartupRotationPropagationSpeed;
        comp.StartupRotationFlexySpeed = StartupRotationFlexySpeed;

        foreach (var r in comp.Manager.Rings) r.transform.localEulerAngles = new Vector3(0, 0, StartupRotationAngle);
    }
}
