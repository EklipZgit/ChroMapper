using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class BasicLightEffect : BasicEventStateManager<BasicLightStateData>
{
    [NonSerialized] public ColorSchemeSO ColorScheme;

    public static readonly float FadeTimeSecond = 1.5f;
    public static readonly float FlashTimeSecond = 0.6f;
    public static float FadeTimeBeat = FadeTimeSecond;
    public static float FlashTimeBeat = FlashTimeSecond;

    [SerializeField] public ColorBoostEffect ColorBoostEffect;

    [SerializeField] private float offIntensity;
    [SerializeField] private bool lightOnStart;
    [SerializeField] private bool invertColorScheme;

    [SerializeField] private List<LightControllerEntry> controllerEntries = new();

    private Dictionary<int, BaseLightController> lightIDToController;
    public Dictionary<int, int> LightIDToLane; // we opt for dict because lightID can be arbitrary value
    [NonSerialized] public int[] LaneToLightID;
    [NonSerialized] public int[][] LaneToLightIDs; // this also refer to propID

    private readonly Dictionary<BaseLightController, (LightColorTween tween,
            BasicEventStateChunksContainer<BasicLightStateData> container)>
        controllerToContainer = new();

    private List<ChromaLiteData> chromaLiteDatas = new();
    private List<ChromaGradientData> chromaGradientDatas = new();

    private void Start()
    {
        CalculateMapping();
        ColorBoostEffect.OnStateChanged += HandleBoostChanged;
    }

    private void OnDestroy() => ColorBoostEffect.OnStateChanged -= HandleBoostChanged;

    public void Register(BaseLightController lightController, int id = -1)
    {
        if (controllerEntries.Exists(l => l.Controller == lightController))
        {
            Debug.LogWarning($"{lightController} is already registered in {this}");
            return;
        }

        if (id != -1 && controllerEntries.Exists(l => l.ID == id))
        {
            Debug.LogError($"ID {id} is already used in {this}");
            return;
        }

        if (id == -1) id = 0;
        while (controllerEntries.Exists(l => l.ID == id)) id++;
        controllerEntries.Add(new() { ID = id, Controller = lightController });
    }

    public void Unregister(BaseLightController lightController) =>
        controllerEntries.RemoveAll(x => x.Controller == lightController);

    private void CalculateMapping()
    {
        var ordered = controllerEntries.OrderBy(x => x.ID).ToList();
        lightIDToController = ordered.ToDictionary(x => x.ID, x => x.Controller);
        LaneToLightID = ordered.Select(x => x.ID).ToArray();
        LaneToLightIDs = ordered
            .GroupBy(x => Mathf.RoundToInt(x.Controller.transform.position.z))
            .OrderBy(x => x.Key)
            .Select(x => x.Select(y => y.ID).ToArray())
            .ToArray();
        LightIDToLane = ordered.ToDictionary(x => x.ID, x => Array.IndexOf(LaneToLightID, x.ID));
    }

    public override void Initialize()
    {
        controllerToContainer.Clear();
        foreach (var controller in controllerEntries.Select(x => x.Controller))
        {
            controllerToContainer[controller] =
                (new(), InitializeStates(new BasicEventStateChunksContainer<BasicLightStateData>()));
            foreach (var state in controllerToContainer[controller].container.Chunks.SelectMany(chunk => chunk))
            {
                if (!lightOnStart) continue;
                state.Base.FloatValue = 1f;
                state.StartAlpha = state.EndAlpha = state.Base.FloatValue * offIntensity;
            }
        }
    }

    public override void UpdateTime(float currentTime)
    {
        foreach (var (lightingObject, (tween, container)) in controllerToContainer)
        {
            if (!container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying))
                UpdateObject(tween, container.CurrentState);

            if (tween.UpdateTime(currentTime)) lightingObject.UpdateColor(tween.Color);
        }
    }

    private void UpdateObject(LightColorTween tween, BasicLightStateData stateData)
    {
        tween.StartTimeAlpha = stateData.StartTime;
        tween.StartTimeColor = stateData.StartTimeColor;
        tween.StartAlpha = stateData.StartAlpha;
        tween.StartColor = stateData.StartChromaColor
            ?? ColorScheme.GetColorFrom(stateData.StartColor, invertColorScheme);

        tween.EndTimeAlpha = stateData.EndTimeAlpha;
        tween.EndTimeColor = stateData.EndTimeColor;
        tween.EndAlpha = stateData.EndAlpha;
        tween.EndColor =
            stateData.EndChromaColor ?? ColorScheme.GetColorFrom(stateData.EndColor, invertColorScheme);

        tween.UseHSV = stateData.UseHSV;
        tween.Easing = stateData.Easing;
    }

    public void UpdateStartAndEndColor(LightColorTween tween, BasicLightStateData stateData)
    {
        tween.StartColor = stateData.StartChromaColor
            ?? ColorScheme.GetColorFrom(stateData.StartColor, invertColorScheme);
        tween.EndColor =
            stateData.EndChromaColor ?? ColorScheme.GetColorFrom(stateData.EndColor, invertColorScheme);
    }

    private void HandleBoostChanged(bool boost)
    {
        foreach (var (_, (tween, container)) in controllerToContainer)
            UpdateStartAndEndColor(tween, container.CurrentState);
    }

    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.red;
    //    if (GroupingMultiplier <= 0.1f) return;
    //    for (var i = -5; i < 150; i++)
    //    {
    //        var z = ((i - GroupingOffset) / GroupingMultiplier) + 0.5f;
    //        Gizmos.DrawLine(new Vector3(-50, 0, z), new Vector3(50, 0, z));
    //    }
    //}

    protected override BasicLightStateData CreateState(BaseEvent data) => new(data);

    public override void BuildFromData(IEnumerable<BaseEvent> dataList)
    {
        foreach (var data in dataList) InsertData(data);
    }

    protected override void OnInsertUpdateToPreviousState(
        BasicLightStateData newStateData,
        BasicLightStateData previousStateData)
    {
        base.OnInsertUpdateToPreviousState(newStateData, previousStateData);

        if (newStateData.Base.IsTransition && IsValidEventToTransition(previousStateData.Base))
        {
            if (previousStateData.Base.IsOff) previousStateData.StartColor = newStateData.StartColor;
            previousStateData.EndTimeAlpha = newStateData.StartTime;
            previousStateData.EndTimeColor = newStateData.StartTime;
            previousStateData.EndColor = newStateData.StartColor;
            previousStateData.EndChromaColor = newStateData.StartChromaColor;
            previousStateData.EndAlpha = newStateData.StartAlpha;
            previousStateData.Easing = Easing.Named(newStateData.Base.CustomEasing ?? "easeLinear");
            previousStateData.UseHSV = newStateData.Base.CustomLerpType == "HSV";
            return;
        }

        previousStateData.EndColor = previousStateData.StartColor;
        // previousState.EndTimeColor = newState.StartTimeColor;
        // previousState.EndChromaColor = previousState.StartChromaColor;

        if (!previousStateData.Base.IsFade && !previousStateData.Base.IsFlash)
        {
            previousStateData.EndTimeAlpha = newStateData.StartTime;
            previousStateData.EndAlpha = previousStateData.StartAlpha;
        }

        if (previousStateData.Base.IsOff)
        {
            previousStateData.StartAlpha =
                previousStateData.EndAlpha = previousStateData.Base.FloatValue * offIntensity;
        }

        if (newStateData.Base.IsOff) newStateData.StartColor = previousStateData.EndColor;
    }

    protected override void OnInsertUpdateFromPreviousStateAndNextState(
        BasicLightStateData newStateData,
        BasicLightStateData previousStateData,
        BasicLightStateData nextStateData)
    {
        if (newStateData.Base.IsOff && !nextStateData.Base.IsTransition)
            newStateData.StartColor = newStateData.EndColor = previousStateData.StartColor;
    }

    protected override void OnInsertUpdateFromNextState(
        BasicLightStateData newStateData,
        BasicLightStateData nextStateData)
    {
        base.OnInsertUpdateFromNextState(newStateData, nextStateData);
        if (nextStateData.Base.IsTransition && IsValidEventToTransition(newStateData.Base))
        {
            if (newStateData.Base.IsOff) newStateData.StartColor = nextStateData.StartColor;
            newStateData.EndTimeAlpha = nextStateData.StartTime;
            newStateData.EndTimeColor = nextStateData.StartTime;
            newStateData.EndColor = nextStateData.StartColor;
            newStateData.EndChromaColor = nextStateData.StartChromaColor;
            newStateData.EndAlpha = nextStateData.StartAlpha;
            newStateData.Easing = Easing.Named(nextStateData.Base.CustomEasing ?? "easeLinear");
            newStateData.UseHSV = nextStateData.Base.CustomLerpType == "HSV";
            return;
        }

        if (!newStateData.Base.IsFade && !newStateData.Base.IsFlash)
            newStateData.EndTimeAlpha = nextStateData.StartTime;
    }

    protected override void OnInsertUpdateToNextState(
        BasicLightStateData newStateData,
        BasicLightStateData nextState)
    {
        if (nextState.Base.IsOff) nextState.StartColor = nextState.EndColor = newStateData.StartColor;
    }

    private void UpdateExistingWithChromaLite(float time)
    {
        var fromIndex = chromaLiteDatas.FindLastIndex(cl => cl.Base.SongBpmTime <= time);
        var from = fromIndex != -1 && fromIndex < chromaLiteDatas.Count
            ? chromaLiteDatas[fromIndex]
            : new ChromaLiteData { Base = new BaseEvent { songBpmTime = float.MinValue } };

        var untilIndex = chromaLiteDatas.FindIndex(cl => cl.Base.SongBpmTime > time);
        var until = untilIndex != -1 ? chromaLiteDatas[untilIndex].Base.SongBpmTime : float.MaxValue;

        foreach (var enumerator in controllerToContainer.Values.Select(c =>
            c.container.EnumerateFrom(from.Base.SongBpmTime)))
        {
            while (enumerator.MoveNext())
            {
                var state = enumerator.Current;
                if (state!.StartTime >= until) break;
                if (state.Base.CustomColor == null) state.StartChromaColor = state.EndChromaColor = from.Color;
            }
        }
    }

    // i would like if chroma gradient just stopped working entirely so i dont have to deal with this shit again
    private void UpdateExistingWithChromaGradient(float startTime, float endTime)
    {
        foreach (var (container, enumerator) in controllerToContainer.Values.Select(c =>
            (c.container, c.container.EnumerateFrom(startTime))))
        {
            while (enumerator.MoveNext())
            {
                var state = enumerator.Current;
                if (state!.StartTime >= endTime) break;

                var fromIndex = chromaGradientDatas.FindLastIndex(cl =>
                    cl.StartTime <= state.StartTime && state.StartTime <= cl.EndTime);
                if (fromIndex == -1)
                {
                    state.StartTimeColor = state.StartTime;
                    state.EndTimeColor = state.EndTime;

                    if (state.Base.IsFlash)
                        state.Easing = Easing.Cubic.Out;
                    else if (state.Base.IsFade)
                        state.Easing = Easing.Exponential.Out;
                    else
                        state.Easing = Easing.Linear;

                    state.StartChromaColor = state.EndChromaColor = null;
                    if (state.Base.CustomColor != null
                        && Settings.Instance.EmulateChromaLite
                        && !state.Base.IsWhite)
                        state.StartChromaColor = state.EndChromaColor = (Color)state.Base.CustomColor;

                    if (chromaLiteDatas.Count > 0)
                    {
                        var chromaLiteIndex =
                            chromaLiteDatas.FindLastIndex(data =>
                                data.Base.SongBpmTime <= state.Base.SongBpmTime);
                        if (chromaLiteIndex != -1 && Settings.Instance.EmulateChromaLite)
                            state.StartChromaColor = state.EndChromaColor = chromaLiteDatas[chromaLiteIndex].Color;
                    }
                }
                else
                {
                    var from = chromaGradientDatas[fromIndex];
                    UpdateStateWithChromaGradient(state, from);
                }

                var (_, _, prevState) = container.GetPreviousStateFrom(state);
                var (_, _, nextState) = container.GetNextStateFrom(state);

                OnInsertUpdateToPreviousState(state, prevState);
                OnInsertUpdateFromPreviousStateAndNextState(state, prevState, nextState);
                OnInsertUpdateFromNextState(state, nextState);
                OnInsertUpdateToNextState(state, nextState);
            }
        }
    }

    private void InsertWithChromaGradient(BasicLightStateData stateData)
    {
        var chromaGradientIndex =
            chromaGradientDatas.FindLastIndex(cg =>
                cg.StartTime <= stateData.StartTime && stateData.StartTime <= cg.EndTime);
        if (chromaGradientIndex != -1)
            UpdateStateWithChromaGradient(stateData, chromaGradientDatas[chromaGradientIndex]);
    }

    private void UpdateStateWithChromaGradient(BasicLightStateData stateData, ChromaGradientData chromaGradientData)
    {
        stateData.StartTimeColor = chromaGradientData.StartTime;
        stateData.EndTimeColor = chromaGradientData.EndTime;
        stateData.StartChromaColor = chromaGradientData.StartColor;
        stateData.EndChromaColor = chromaGradientData.EndColor;
        stateData.Easing = chromaGradientData.Easing;
    }

    public override void InsertData(BaseEvent data)
    {
        Color? chromaColor = null;

        // Check if its a legacy Chroma RGB event
        switch (data.Value)
        {
            case >= ColourManager.RgbintOffset when Settings.Instance.EmulateChromaLite:
                {
                    chromaLiteDatas.Add(
                        new() { Base = data, Color = ColourManager.ColourFromInt(data.Value) });
                    chromaLiteDatas = chromaLiteDatas.OrderBy(cl => cl.Base.SongBpmTime).ToList();
                    UpdateExistingWithChromaLite(data.SongBpmTime);
                    return;
                }
            case ColourManager.RGBReset when Settings.Instance.EmulateChromaLite:
                {
                    chromaLiteDatas.Add(new() { Base = data, Color = null });
                    chromaLiteDatas = chromaLiteDatas.OrderBy(cl => cl.Base.SongBpmTime).ToList();
                    UpdateExistingWithChromaLite(data.SongBpmTime);
                    return; // this was a break, not sure why
                }
        }

        //Check if it is a PogU new Chroma event
        if (data.CustomColor != null
            && Settings.Instance.EmulateChromaLite
            && !data.IsWhite) // White overrides Chroma
            chromaColor = (Color)data.CustomColor;

        if (chromaLiteDatas.Count > 0)
        {
            var chromaLiteIndex = chromaLiteDatas.FindLastIndex(d => d.Base.SongBpmTime <= data.SongBpmTime);
            if (chromaLiteIndex != -1 && Settings.Instance.EmulateChromaLite)
                chromaColor = chromaLiteDatas[chromaLiteIndex].Color;
        }

        if (data.CustomLightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            chromaGradientDatas.Add(
                new ChromaGradientData
                {
                    Base = data,
                    StartTime = data.SongBpmTime,
                    EndTime =
                        data.SongBpmTime
                        + data.CustomLightGradient.Duration, // TODO: duration is not actual song bpm time
                    StartColor = data.CustomLightGradient.StartColor,
                    EndColor = data.CustomLightGradient.EndColor,
                    Easing = Easing.Named(data.CustomLightGradient.EasingType)
                });
            chromaGradientDatas = chromaGradientDatas.OrderBy(cl => cl.StartTime).ToList();
            UpdateExistingWithChromaGradient(data.SongBpmTime, data.SongBpmTime + data.CustomLightGradient.Duration);
        }

        //Check to see if we're soloing any particular event
        // wtf is solo event
        // if (SoloAnEventType && data.Type != SoloEventType) mainColor = invertedColor = Color.black.WithAlpha(0);

        var affectedLights = lightIDToController.Values.AsEnumerable();
        if (data.CustomLightID != null && lightIDToController != null && Settings.Instance.EmulateChromaAdvanced)
        {
            var lightIDArr = data.CustomLightID;
            var filteredLights = new List<BaseLightController>(lightIDArr.Length);
            foreach (var lightID in lightIDArr)
            {
                if (!lightIDToController.TryGetValue(lightID, out var lightingObject)) continue;
                filteredLights.Add(lightingObject);
            }

            affectedLights = filteredLights;
        }

        foreach (var lightingObject in affectedLights)
        {
            var newState = CreateState(data);
            newState.StartTime = data.SongBpmTime;
            newState.StartTimeColor = data.SongBpmTime;
            newState.StartColor = InferColorFromEvent(data);
            newState.StartChromaColor = chromaColor;
            newState.StartAlpha = data.FloatValue;
            newState.EndTime = float.MaxValue;
            newState.EndTimeAlpha = float.MaxValue;
            newState.EndTimeColor = float.MaxValue;
            newState.EndColor = InferColorFromEvent(data);
            newState.EndChromaColor = chromaColor;
            newState.EndAlpha = data.FloatValue;

            if (data.IsOff)
                newState.StartAlpha = newState.EndAlpha = data.FloatValue * offIntensity;
            else if (data.IsFlash)
            {
                newState.EndTimeAlpha = newState.StartTime + FlashTimeBeat;
                newState.StartAlpha = data.FloatValue * 1.2f;
                newState.EndAlpha = data.FloatValue;
                newState.Easing = Easing.Cubic.Out;
            }
            else if (data.IsFade)
            {
                newState.EndTimeAlpha = newState.StartTime + FadeTimeBeat;
                newState.StartAlpha = data.FloatValue * 1.2f;
                newState.EndAlpha = 0f;
                newState.Easing = Easing.Exponential.Out;
                newState.EndAlpha = data.FloatValue * offIntensity;
            }

            InsertWithChromaGradient(newState);

            var (tween, container) = controllerToContainer[lightingObject];

            // let's assume this will be previous state if this is inserted within the range
            var previousState = container.CurrentState;
            var previousValid = previousState.IsWithinRange(data.SongBpmTime);
            HandleInsertState(container, newState);

            if (!previousValid) continue;
            container.SetStateAt(Atsc.CurrentSongBpmTime);
            UpdateObject(tween, container.CurrentState);
        }
    }

    public override void RemoveData(BaseEvent data, BaseEvent original)
    {
        switch (original.Value)
        {
            case >= ColourManager.RgbintOffset when Settings.Instance.EmulateChromaLite:
            case ColourManager.RGBReset when Settings.Instance.EmulateChromaLite:
                {
                    var d = chromaLiteDatas.Find(d => d.Base == data);
                    chromaLiteDatas.Remove(d);
                    UpdateExistingWithChromaLite(original.SongBpmTime);
                    return;
                }
        }

        if (original.CustomLightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            var d = chromaGradientDatas.Find(d => d.Base == data);
            chromaGradientDatas.Remove(d);
            UpdateExistingWithChromaGradient(
                original.SongBpmTime,
                original.SongBpmTime + original.CustomLightGradient.Duration);
        }

        var affectedLights = lightIDToController.Values.AsEnumerable();

        if (original.CustomLightID != null && lightIDToController != null && Settings.Instance.EmulateChromaAdvanced)
        {
            var lightIDArr = original.CustomLightID;
            var filteredLights = new List<BaseLightController>(lightIDArr.Length);
            foreach (var lightID in lightIDArr)
            {
                if (!lightIDToController.TryGetValue(lightID, out var lightingObject)) continue;
                filteredLights.Add(lightingObject);
            }

            affectedLights = filteredLights;
        }

        foreach (var lightingObject in affectedLights)
        {
            var (tween, container) = controllerToContainer[lightingObject];
            HandleRemoveState(container, data, original);

            // unfortunately, we cannot do the same as insertion so we need to search
            var (_, _, previousState) = container.GetStateAt(Atsc.CurrentSongBpmTime);
            if (!previousState.IsWithinRange(data.SongBpmTime)) continue;
            container.SetStateAt(Atsc.CurrentSongBpmTime);
            UpdateObject(tween, container.CurrentState);
        }
    }

    protected override void
        OnRemoveUpdatePreviousAndNextState(
            BasicLightStateData currentStateData,
            BasicLightStateData previousStateData,
            BasicLightStateData nextStateData)
    {
        base.OnRemoveUpdatePreviousAndNextState(currentStateData, previousStateData, nextStateData);
        if (nextStateData.Base.IsTransition && IsValidEventToTransition(previousStateData.Base))
        {
            if (previousStateData.Base.IsOff) previousStateData.StartColor = nextStateData.StartColor;
            previousStateData.EndTimeAlpha = nextStateData.StartTime;
            previousStateData.EndTimeColor = nextStateData.StartTimeColor;
            previousStateData.EndColor = nextStateData.StartColor;
            previousStateData.EndChromaColor = nextStateData.StartChromaColor;
            previousStateData.EndAlpha = nextStateData.StartAlpha;
            previousStateData.Easing = Easing.Named(nextStateData.Base.CustomEasing ?? "easeLinear");
            previousStateData.UseHSV = nextStateData.Base.CustomLerpType == "HSV";
        }
        else
        {
            previousStateData.EndTimeColor = nextStateData.StartTimeColor;
            previousStateData.EndColor = previousStateData.StartColor;
            previousStateData.EndChromaColor = previousStateData.StartChromaColor;

            if (!previousStateData.Base.IsFade && !previousStateData.Base.IsFlash)
            {
                previousStateData.EndTimeAlpha = nextStateData.StartTime;
                previousStateData.EndAlpha = previousStateData.StartAlpha;
            }

            if (previousStateData.Base.IsOff)
            {
                previousStateData.StartAlpha =
                    previousStateData.EndAlpha = previousStateData.Base.FloatValue * offIntensity;
            }

            if (nextStateData.Base.IsOff) nextStateData.StartColor = previousStateData.EndColor;
        }

        InsertWithChromaGradient(previousStateData);
        InsertWithChromaGradient(nextStateData);
    }

    public override void UpdateDirty()
    {
        foreach (var (tween, container) in controllerToContainer.Values) UpdateObject(tween, container.CurrentState);
    }

    private static LightColor InferColorFromEvent(BaseEvent evt) =>
        evt.IsBlue ? LightColor.Blue : evt.IsRed ? LightColor.Red : LightColor.White;

    private static bool IsValidEventToTransition(BaseEvent evt) => evt.IsOn || evt.IsOff || evt.IsTransition;

    public struct ChromaLiteData : IEquatable<ChromaLiteData>
    {
        public BaseEvent Base;
        public Color? Color;

        public bool Equals(ChromaLiteData other) => Equals(Base, other.Base);
        public override bool Equals(object obj) => obj is ChromaLiteData other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Base, Color);
    }

    public struct ChromaGradientData : IEquatable<ChromaGradientData>
    {
        public BaseEvent Base;
        public float StartTime;
        public float EndTime;
        public Color StartColor;
        public Color EndColor;
        public Func<float, float> Easing;

        public bool Equals(ChromaGradientData other) => Equals(Base, other.Base);
        public override bool Equals(object obj) => obj is ChromaGradientData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Base, StartTime, EndTime, StartColor, EndColor, Easing);
    }
}
