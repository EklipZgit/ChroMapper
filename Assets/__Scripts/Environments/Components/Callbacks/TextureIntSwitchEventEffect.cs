using System;
using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public class TextureIntSwitchEventEffect : BasicEventEffect<TextureSwitchStateData>
{
    [Serializable]
    private struct TextureValueTuple
    {
        public int value;
        public Texture texture;
    }

    [SerializeField]
    private MaterialPropertyBlockController _materialPropertyBlockController;
    [SerializeField]
    private string _texturePropertyName;
    [Space]
    [SerializeField]
    private int _defaultIndex;
    [SerializeField]
    private TextureValueTuple[] _textureValueTuples;

    private int _texturePropertyId;
    private Dictionary<int, Texture> _valueToTextureMap;

    private readonly BasicEventStateChunksContainer<TextureSwitchStateData> container = new();

    private void Awake()
    {
        _texturePropertyId = Shader.PropertyToID(_texturePropertyName);
        _valueToTextureMap = new Dictionary<int, Texture>();
        foreach (var tuple in _textureValueTuples)
            _valueToTextureMap[tuple.value] = tuple.texture;
    }

    public override void Initialize() => InitializeStates(container);

    public override void Refresh() => UpdateObject(container.CurrentState);

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, isPlaying))
            UpdateObject(container.CurrentState);
    }

    private void UpdateObject(TextureSwitchStateData stateData)
    {
        SetTextureByValue(stateData.TextureValue);
    }

    protected override TextureSwitchStateData CreateState(BaseEvent data) =>
        new(data) { TextureValue = _defaultIndex };

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        state.TextureValue = data.Value;
        HandleInsertState(container, state);
    }

    public override void RemoveData(BaseEvent reference, BaseEvent original)
    {
        var state = container.GetStateFrom(reference, original);
        HandleRemoveState(container, state);

        if (container.CurrentState != state) return;
        container.SetStateAt(reference.SongBpmTime);
        UpdateObject(container.CurrentState);
    }

    private void SetTextureByValue(int value)
    {
        if (!_valueToTextureMap.TryGetValue(value, out var texture))
            texture = _valueToTextureMap[_defaultIndex];

        _materialPropertyBlockController.Mpb.SetTexture(_texturePropertyId, texture);
        _materialPropertyBlockController.ApplyChanges();
    }
}

public class TextureSwitchStateData : BasicEventStateData
{
    public int TextureValue;

    public TextureSwitchStateData(BaseEvent data) : base(data) { }
}