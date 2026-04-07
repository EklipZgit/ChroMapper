using UnityEngine;

public class FloatSDFPointScaleEffect : FxTarget
{
	[SerializeField]
	private SDFPoint _colorPoints;

	[Space]
	[SerializeField]
	private Vector2 _valueBounds = new Vector2(1f, 10f);

	private float _startScale;

	protected void Awake()
	{
		_startScale = 1f;
	}

	public override void SetValue(int groupId, int elementId, float value)
	{
		Scale(value);
	}

	public override void TriggerValue(int groupId, int elementId, float value)
	{
		Scale(value);
	}

	private void Scale(float value)
	{
		_colorPoints.sqrtRadius = _startScale * Mathf.Clamp(value, _valueBounds.x, _valueBounds.y);
	}
}
