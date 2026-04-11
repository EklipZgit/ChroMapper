using UnityEngine;

public class SpringJointData : EnvironmentComponentData<SpringJoint>
{
    public string connectedBody;
    public string connectedArticulationBody;
    public Vector3 anchor;
    public bool autoConfigureConnectedAnchor;
    public Vector3 connectedAnchor;
    public float spring;
    public float damper;
    public float minDistance;
    public float maxDistance;
    public float tolerance;
    public string breakForce;
    public string breakTorque;
    public bool enableCollision;
    public bool enablePreprocessing;
    public float massScale;
    public float connectedMassScale;

    public override void SearchAndFillComponents(GameObject self, SpringJoint comp, CreateContainer container)
    {
        comp.connectedBody = container.GetGameObjectOrNull(connectedBody, self).GetComponent<Rigidbody>();
        // comp.connectedArticulationBody = container.GetGameObjectOrNull(connectedBody, self).GetComponent<ArticulationBody>();
    }

    public override void CopyTo(SpringJoint comp)
    {
        comp.anchor = anchor;
        comp.autoConfigureConnectedAnchor = autoConfigureConnectedAnchor;
        comp.connectedAnchor = connectedAnchor;
        comp.spring = spring;
        comp.damper = damper;
        comp.minDistance = minDistance;
        comp.maxDistance = maxDistance;
        comp.tolerance = tolerance;
        comp.breakForce = float.Parse(breakForce);
        comp.breakTorque = float.Parse(breakTorque);
        comp.enableCollision = enableCollision;
        comp.enablePreprocessing = enablePreprocessing;
        comp.massScale = massScale;
        comp.connectedMassScale = connectedMassScale;
    }
}
