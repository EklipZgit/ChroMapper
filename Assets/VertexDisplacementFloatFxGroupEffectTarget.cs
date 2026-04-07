using UnityEngine;

public class VertexDisplacementFloatFxGroupEffectTarget : FxTarget
{
    [SerializeField]
    private Vector3 _displacementRanges;
    [SerializeField]
    private AnimationCurve _xAnimationCurve;
    [SerializeField]
    private AnimationCurve _yAnimationCurve;
    [SerializeField]
    private AnimationCurve _zAnimationCurve;
    [SerializeField]
    private MaterialPropertyBlockController _displacementController;
    [SerializeField]
    private Renderer _renderer;
    [SerializeField]
    private bool _useTestValue;
    [SerializeField]
    private float _testFloatValue;

    private static readonly int _vertexDisplacementRangeVectorPropertyID = Shader.PropertyToID("_DisplacementAxisMultiplier");
    private readonly Bounds _bounds = new Bounds(Vector3.zero, 1000f * Vector3.one);

    protected void Awake()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        if (_displacementController == null)
            _displacementController = GetComponent<MaterialPropertyBlockController>();
    }

    protected void OnEnable()
    {
        _renderer.bounds = _bounds;
        _displacementController.Mpb.SetVector(_vertexDisplacementRangeVectorPropertyID, CalculateDisplacementVector(0f));
    }

    protected void OnValidate()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();

        if (_displacementController == null)
            _displacementController = GetComponent<MaterialPropertyBlockController>();

        if (_useTestValue)
            SetValue(0, 0, _testFloatValue);
    }

    private Vector4 CalculateDisplacementVector(float value)
    {
        float x = _xAnimationCurve.Evaluate(value) * _displacementRanges.x;
        float y = _yAnimationCurve.Evaluate(value) * _displacementRanges.y;
        float z = _zAnimationCurve.Evaluate(value) * _displacementRanges.z;
        return new Vector4(x, y, z, 1f);
    }

    public override void SetValue(int groupId, int elementId, float value)
    {
        SetValue(value);
    }

    public override void TriggerValue(int groupId, int elementId, float value)
    {
        SetValue(value);
    }

    private void SetValue(float value)
    {
        Vector4 value2 = CalculateDisplacementVector(value);
        _displacementController.Mpb.SetVector(_vertexDisplacementRangeVectorPropertyID, value2);
        _displacementController.ApplyChanges();
    }
}