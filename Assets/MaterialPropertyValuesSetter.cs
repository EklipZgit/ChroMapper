using System;
using UnityEngine;

public class MaterialPropertyValuesSetter : MonoBehaviour
{
	[Serializable]
	public class PropertyValuePairBase
	{
		[SerializeField]
		private string _propertyName;

		public int propertyId { get; private set; }

		public PropertyValuePairBase()
		{
			RefreshPropertyId();
		}

		public void RefreshPropertyId()
		{
			propertyId = Shader.PropertyToID(_propertyName);
		}
	}

	[Serializable]
	public class PropertyNameFloatValuePair : PropertyValuePairBase
	{
		public float value;
	}

	[Serializable]
	public class PropertyNameIntValuePair : PropertyValuePairBase
	{
		public int value;
	}

	[Serializable]
	public class PropertyNameVectorValuePair : PropertyValuePairBase
	{
		public Vector4 vector;
	}

	[Serializable]
	public class PropertyNameColorValuePair : PropertyValuePairBase
	{
		public Color color;
	}

	[SerializeField]
	private MaterialPropertyBlockController _materialPropertyBlockController;

	[SerializeField]
	private PropertyNameFloatValuePair[] _floats;

	[SerializeField]
	private PropertyNameVectorValuePair[] _vectors;

	[SerializeField]
	private PropertyNameColorValuePair[] _colors;

	[SerializeField]
	private PropertyNameIntValuePair[] _ints;

	protected void Start()
	{
		RefreshPropertyIds();
		ApplyParams();
	}

	protected void OnValidate()
	{
		if (_materialPropertyBlockController == null)
		{
			_materialPropertyBlockController = GetComponent<MaterialPropertyBlockController>();
		}
		RefreshPropertyIds();
		ApplyParams();
	}

	private void RefreshPropertyIds()
	{
		if (_floats != null)
		{
			PropertyNameFloatValuePair[] floats = _floats;
			for (int i = 0; i < floats.Length; i++)
			{
				floats[i].RefreshPropertyId();
			}
		}
		if (_vectors != null)
		{
			PropertyNameVectorValuePair[] vectors = _vectors;
			for (int i = 0; i < vectors.Length; i++)
			{
				vectors[i].RefreshPropertyId();
			}
		}
		if (_colors != null)
		{
			PropertyNameColorValuePair[] colors = _colors;
			for (int i = 0; i < colors.Length; i++)
			{
				colors[i].RefreshPropertyId();
			}
		}
		if (_ints != null)
		{
			PropertyNameIntValuePair[] ints = _ints;
			for (int i = 0; i < ints.Length; i++)
			{
				ints[i].RefreshPropertyId();
			}
		}
	}

	private void ApplyParams()
	{
		if (_floats != null)
		{
			PropertyNameFloatValuePair[] floats = _floats;
			foreach (PropertyNameFloatValuePair propertyNameFloatValuePair in floats)
			{
				_materialPropertyBlockController.Mpb.SetFloat(propertyNameFloatValuePair.propertyId, propertyNameFloatValuePair.value);
			}
		}
		if (_vectors != null)
		{
			PropertyNameVectorValuePair[] vectors = _vectors;
			foreach (PropertyNameVectorValuePair propertyNameVectorValuePair in vectors)
			{
				_materialPropertyBlockController.Mpb.SetVector(propertyNameVectorValuePair.propertyId, propertyNameVectorValuePair.vector);
			}
		}
		if (_colors != null)
		{
			PropertyNameColorValuePair[] colors = _colors;
			foreach (PropertyNameColorValuePair propertyNameColorValuePair in colors)
			{
				_materialPropertyBlockController.Mpb.SetVector(propertyNameColorValuePair.propertyId, propertyNameColorValuePair.color);
			}
		}
		if (_ints != null)
		{
			PropertyNameIntValuePair[] ints = _ints;
			foreach (PropertyNameIntValuePair propertyNameIntValuePair in ints)
			{
				_materialPropertyBlockController.Mpb.SetInt(propertyNameIntValuePair.propertyId, propertyNameIntValuePair.value);
			}
		}
		_materialPropertyBlockController.ApplyChanges();
	}
}
