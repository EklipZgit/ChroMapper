using UnityEngine;

public class SDFPoint : MonoBehaviour
{
    [Tooltip("Use negative radius for the SDF to act as a negative force (multiplying rather than subtracting)")]
    [SerializeField]
    private float _radius = 1f;
    private float _sqrtRadius = 1f;

    public float sqrtRadius
    {
        get
        {
            return _sqrtRadius;
        }
        set
        {
            _sqrtRadius = value;
        }
    }
}