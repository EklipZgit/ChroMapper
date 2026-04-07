using UnityEngine;

[ExecuteAlways]
public class SDFArrayManager : MonoBehaviour
{
	[SerializeField]
	private SDFPoint[] _sdfPointArray;

	private Vector4[] _sdfArrayValues;

	private bool _isInitialized;

	private static readonly int _sdfPointsArray = Shader.PropertyToID("_SDFPointArray");

	protected void Awake()
	{
		InitIfNeeded();
	}

	private void InitIfNeeded()
	{
		if (!_isInitialized)
		{
			_isInitialized = true;
			_sdfArrayValues = new Vector4[_sdfPointArray.Length];
		}
	}

	protected void Update()
	{
		InitIfNeeded();
		for (int i = 0; i < _sdfPointArray.Length; i++)
		{
			Vector3 position = _sdfPointArray[i].transform.position;
			_sdfArrayValues[i] = new Vector4(position.x, position.y, position.z, _sdfPointArray[i].sqrtRadius);
		}
		Shader.SetGlobalVectorArray(_sdfPointsArray, _sdfArrayValues);
	}
}
