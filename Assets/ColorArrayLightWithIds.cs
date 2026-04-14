using System;
using UnityEngine;

[ExecuteAlways]
public class ColorArrayLightWithIds : MonoBehaviour
{
    [Serializable]
    public class ColorArrayLightWithId
    {
        [SerializeField]
        private int _index;

        public event Action<int, Color> didSetColorEvent;

        public ColorArrayLightWithId(int index)
        {
            _index = index;
        }

        public void ColorWasSet(Color newColor)
        {
            didSetColorEvent?.Invoke(_index, newColor);
        }
    }

    [SerializeField]
    private int _idCount = 1;

    [SerializeField, HideInInspector]
    private ColorArrayLightWithId[] _colorArrayLightWithIds = Array.Empty<ColorArrayLightWithId>();

    [Space]
    [SerializeField]
    private MaterialController _materialController;

    [SerializeField]
    private MaterialPropertyBlockController[] _materialPropertyBlockControllers;

    [Space]
    [SerializeField]
    private string _colorsArrayPropertyName = "_ColorsArray";

    [SerializeField]
    private string _colorsArrayOffsetPropertyName = "_ColorsArrayOffset";

    private int _colorsArrayPropertyId;
    private int _colorsArrayOffsetPropertyId;
    private Vector4[] _colorsArray;

    private void OnValidate()
    {
        _idCount = Math.Max(1, _idCount);
        RebuildEntries();
    }

    private void OnEnable()
    {
        RebuildEntries();
        RegisterArrayForColorChanges();
    }

    private void OnDestroy()
    {
        UnregisterArrayFromColorChanges();
    }

    private void RebuildEntries()
    {
        // Grow or shrink _colorArrayLightWithIds to match _idCount,
        // preserving existing entries so events are not lost at runtime.
        var old = _colorArrayLightWithIds ?? Array.Empty<ColorArrayLightWithId>();
        if (old.Length == _idCount) return;

        var updated = new ColorArrayLightWithId[_idCount];
        for (int i = 0; i < _idCount; i++)
            updated[i] = i < old.Length ? old[i] : new ColorArrayLightWithId(i);

        _colorArrayLightWithIds = updated;
    }

    private void HandleColorLightWithIdDidSetColor(int index, Color color)
    {
        color = color.linear;
        _colorsArray[index] = new Vector4(color.r, color.g, color.b, color.a);
    }

    private void SetColorDataToMaterial()
    {
        if (_materialController == null || _colorsArray == null) return;
        _materialController.material.SetVectorArray(_colorsArrayPropertyId, _colorsArray);
    }

    private void SetColorArrayOffsetToMaterialPropertyBlocks()
    {
        if (_materialPropertyBlockControllers == null || _materialPropertyBlockControllers.Length == 0) return;
        int num = _colorArrayLightWithIds.Length / _materialPropertyBlockControllers.Length;
        for (int i = 0; i < _materialPropertyBlockControllers.Length; i++)
        {
            _materialPropertyBlockControllers[i].Mpb.SetInt(_colorsArrayOffsetPropertyId, i * num);
            _materialPropertyBlockControllers[i].ApplyChanges();
        }
    }

    private void RegisterArrayForColorChanges()
    {
        if (_colorArrayLightWithIds == null || _colorArrayLightWithIds.Length == 0) return;

        _colorsArrayPropertyId = Shader.PropertyToID(_colorsArrayPropertyName);
        _colorsArrayOffsetPropertyId = Shader.PropertyToID(_colorsArrayOffsetPropertyName);
        _colorsArray = new Vector4[_colorArrayLightWithIds.Length];
        for (int i = 0; i < _colorsArray.Length; i++)
        {
            _colorsArray[i] = Vector4.zero;
            _colorArrayLightWithIds[i].didSetColorEvent += HandleColorLightWithIdDidSetColor;
        }
        SetColorArrayOffsetToMaterialPropertyBlocks();
        SetColorDataToMaterial();
    }

    private void UnregisterArrayFromColorChanges()
    {
        if (_colorArrayLightWithIds == null) return;
        for (int i = 0; i < _colorArrayLightWithIds.Length; i++)
            _colorArrayLightWithIds[i].didSetColorEvent -= HandleColorLightWithIdDidSetColor;
    }

    /// <summary>
    /// Returns the ColorArrayLightWithId at the given index so a
    /// ColorArrayLightController can fire ColorWasSet on it directly.
    /// </summary>
    public ColorArrayLightWithId GetLightWithIdAtIndex(int index)
    {
        if (index < 0 || index >= _colorArrayLightWithIds.Length)
        {
            Debug.LogError($"{name}: Index {index} is out of range (length {_colorArrayLightWithIds.Length}).");
            return null;
        }
        return _colorArrayLightWithIds[index];
    }

    /// <summary>
    /// Pushes the current _colorsArray state to the material.
    /// Call this after one or more ColorWasSet calls to batch the GPU upload.
    /// </summary>
    public void FlushToMaterial()
    {
        SetColorDataToMaterial();
    }
}