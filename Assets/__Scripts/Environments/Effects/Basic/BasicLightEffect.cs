using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class BasicLightEffect : BasicEventEffect<BasicLightStateData>
{
    [SerializeField] public ColorBoostEffect ColorBoostEffect;
    [NonSerialized] public ColorSchemeSO ColorScheme;

    [SerializeField] public float OffIntensity;
    [SerializeField] public bool LightOnStart;
    [SerializeField] public bool InvertColorScheme;

    public static readonly float FadeTimeSecond = 1.5f;
    public static readonly float FlashTimeSecond = 0.6f;
    public static float FadeTimeBeat = FadeTimeSecond;
    public static float FlashTimeBeat = FlashTimeSecond;

    [SerializeField] public List<Vector2> LightIdRemapEntries = new();
    [SerializeField] private List<LightController> lightEntries = new();
    private readonly Dictionary<int, int> lightIdRemap = new();
    private readonly Dictionary<int, LightController> lightIDToController = new();
    public readonly Dictionary<int, int> LightIDToLane = new();

    public readonly List<int> LaneToLightID = new();
    public readonly List<int[]> LaneToLightIDs = new(); // this also refer to propID

    private readonly Dictionary<LightController, (LightColorTween tween,
            BasicEventStateChunksContainer<BasicLightStateData> container)>
        controllerToContainer = new();

    private List<ChromaLiteData> chromaLiteData = new();
    private List<ChromaGradientData> chromaGradientData = new();

    private void Start() => ColorBoostEffect.OnStateChanged += HandleBoostChanged;
    private void OnDestroy() => ColorBoostEffect.OnStateChanged -= HandleBoostChanged;

    public void Register(LightController controller, bool strict = true)
    {
        LightController overlight = null;
        if (lightEntries.Exists(l => l == controller))
        {
            Debug.LogWarning($"{controller} is already registered in {this}");
            return;
        }

        if (strict && controller.ID != -1 && lightEntries.Exists(l => l.ID == controller.ID))
        {
            overlight = lightEntries.First(l => l.ID == controller.ID);
            var marker = controller.GetComponent<ChromaIDMarker>();
            var overmarker = overlight.GetComponent<ChromaIDMarker>();
            Debug.LogError(
                $"{marker.ChromaID} {controller.Type}:{controller.ID} is already used by:\n{overmarker.ChromaID} {overlight.Type}:{overlight.ID}; re-registering occupied as new");
            Unregister(overlight);
            overlight.ID = -1;
        }

        if (controller.ID == -1) controller.ID = 0;
        while (lightEntries.Exists(l => l.ID == controller.ID)) controller.ID++;
        lightEntries.Add(controller);
        if (overlight != null) Register(overlight, false);
    }

    public void Unregister(LightController controller) => lightEntries.Remove(controller);

    private void CalculateMapping()
    {
        LaneToLightID.Clear();
        LaneToLightIDs.Clear();
        LightIDToLane.Clear();
        lightIDToController.Clear();
        lightIdRemap.Clear();
        foreach (var lightId in LightIdRemapEntries) lightIdRemap[(int)lightId.x] = (int)lightId.y;

        var ordered = lightEntries.OrderBy(x => x.ID).ToList();
        foreach (var x in ordered) lightIDToController[x.ID] = x;
        LaneToLightID.AddRange(ordered.Select(x => x.ID));
        LaneToLightIDs.AddRange(
            ordered
                .GroupBy(x => Mathf.RoundToInt(x.transform.position.z))
                .OrderBy(x => x.Key)
                .Select(x => x.Select(y => y.ID).ToArray()));
        foreach (var x in ordered) LightIDToLane[x.ID] = LaneToLightID.IndexOf(x.ID);
    }

    public override void Initialize()
    {
        CalculateMapping();
        controllerToContainer.Clear();
        foreach (var controller in lightEntries.Select(x => x))
        {
            controllerToContainer[controller] =
                (new(), InitializeStates(new BasicEventStateChunksContainer<BasicLightStateData>()));
            foreach (var state in controllerToContainer[controller].container.Collection.Select(chunk => chunk))
            {
                if (!LightOnStart) continue;
                state.Base.FloatValue = 1f;
                state.StartAlpha = state.EndAlpha = state.Base.FloatValue * OffIntensity;
            }
        }
    }

    public override void Refresh()
    {
        foreach (var (controller, (tween, _)) in controllerToContainer) controller.SetColor(tween.Color);
    }

    public override void UpdateTime(float currentTime)
    {
        foreach (var (controller, (tween, container)) in controllerToContainer)
        {
            if (!container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying))
                UpdateObject(tween, container.CurrentState);

            if (tween.UpdateTime(currentTime)) controller.SetColor(tween.Color);
        }
    }

    private void UpdateObject(LightColorTween tween, BasicLightStateData stateData)
    {
        tween.StartTimeAlpha = stateData.StartTime;
        tween.StartTimeColor = stateData.StartTimeColor;
        tween.StartAlpha = stateData.StartAlpha;
        tween.StartColor = stateData.StartChromaColor
            ?? ColorScheme.GetColorFrom(stateData.StartColor, InvertColorScheme);

        tween.EndTimeAlpha = stateData.EndTimeAlpha;
        tween.EndTimeColor = stateData.EndTimeColor;
        tween.EndAlpha = stateData.EndAlpha;
        tween.EndColor =
            stateData.EndChromaColor ?? ColorScheme.GetColorFrom(stateData.EndColor, InvertColorScheme);

        tween.UseHSV = stateData.UseHSV;
        tween.Easing = stateData.Easing;
    }

    public void UpdateStartAndEndColor(LightColorTween tween, BasicLightStateData stateData)
    {
        tween.StartColor = stateData.StartChromaColor
            ?? ColorScheme.GetColorFrom(stateData.StartColor, InvertColorScheme);
        tween.EndColor =
            stateData.EndChromaColor ?? ColorScheme.GetColorFrom(stateData.EndColor, InvertColorScheme);
    }

    private void HandleBoostChanged(bool boost)
    {
        foreach (var (_, (tween, container)) in controllerToContainer)
            UpdateStartAndEndColor(tween, container.CurrentState);
    }

    protected override BasicLightStateData CreateState(BaseEvent data) => new(data);

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
                previousStateData.EndAlpha = previousStateData.Base.FloatValue * OffIntensity;
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
        var fromIndex = chromaLiteData.FindLastIndex(cl => cl.Base.SongBpmTime <= time);
        var from = fromIndex != -1 && fromIndex < chromaLiteData.Count
            ? chromaLiteData[fromIndex]
            : new ChromaLiteData { Base = new BaseEvent { songBpmTime = float.MinValue } };

        var untilIndex = chromaLiteData.FindIndex(cl => cl.Base.SongBpmTime > time);
        var until = untilIndex != -1 ? chromaLiteData[untilIndex].Base.SongBpmTime : float.MaxValue;

        foreach (var enumerator in controllerToContainer.Values.Select(c =>
            c.container.Collection.EnumerateFrom(from.Base.SongBpmTime)))
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
            (c.container, c.container.Collection.EnumerateFrom(startTime))))
        {
            while (enumerator.MoveNext())
            {
                var state = enumerator.Current;
                if (state!.StartTime >= endTime) break;

                var fromIndex = chromaGradientData.FindLastIndex(cl =>
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

                    if (chromaLiteData.Count > 0)
                    {
                        var chromaLiteIndex =
                            chromaLiteData.FindLastIndex(data =>
                                data.Base.SongBpmTime <= state.Base.SongBpmTime);
                        if (chromaLiteIndex != -1 && Settings.Instance.EmulateChromaLite)
                            state.StartChromaColor = state.EndChromaColor = chromaLiteData[chromaLiteIndex].Color;
                    }
                }
                else
                {
                    var from = chromaGradientData[fromIndex];
                    UpdateStateWithChromaGradient(state, from);
                }

                var prevState = container.GetPreviousStateFrom(state);
                var nextState = container.GetNextStateFrom(state);

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
            chromaGradientData.FindLastIndex(cg =>
                cg.StartTime <= stateData.StartTime && stateData.StartTime <= cg.EndTime);
        if (chromaGradientIndex != -1)
            UpdateStateWithChromaGradient(stateData, chromaGradientData[chromaGradientIndex]);
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
                    chromaLiteData.Add(
                        new() { Base = data, Color = ColourManager.ColourFromInt(data.Value) });
                    chromaLiteData = chromaLiteData.OrderBy(cl => cl.Base.SongBpmTime).ToList();
                    UpdateExistingWithChromaLite(data.SongBpmTime);
                    return;
                }
            case ColourManager.RGBReset when Settings.Instance.EmulateChromaLite:
                {
                    chromaLiteData.Add(new() { Base = data, Color = null });
                    chromaLiteData = chromaLiteData.OrderBy(cl => cl.Base.SongBpmTime).ToList();
                    UpdateExistingWithChromaLite(data.SongBpmTime);
                    return; // this was a break, not sure why
                }
        }

        //Check if it is a PogU new Chroma event
        if (data.CustomColor != null
            && Settings.Instance.EmulateChromaLite
            && !data.IsWhite) // White overrides Chroma
            chromaColor = (Color)data.CustomColor;

        if (chromaLiteData.Count > 0)
        {
            var chromaLiteIndex = chromaLiteData.FindLastIndex(d => d.Base.SongBpmTime <= data.SongBpmTime);
            if (chromaLiteIndex != -1 && Settings.Instance.EmulateChromaLite)
                chromaColor = chromaLiteData[chromaLiteIndex].Color;
        }

        if (data.CustomLightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            chromaGradientData.Add(
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
            chromaGradientData = chromaGradientData.OrderBy(cl => cl.StartTime).ToList();
            UpdateExistingWithChromaGradient(data.SongBpmTime, data.SongBpmTime + data.CustomLightGradient.Duration);
        }

        //Check to see if we're soloing any particular event
        // wtf is solo event
        // if (SoloAnEventType && data.Type != SoloEventType) mainColor = invertedColor = Color.black.WithAlpha(0);

        var affectedLights = data.CustomLightID != null && Settings.Instance.EmulateChromaAdvanced
            ? GetLightControllerFromLightIds(data)
            : lightIDToController.Values.AsEnumerable();

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
                newState.StartAlpha = newState.EndAlpha = data.FloatValue * OffIntensity;
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
                newState.EndAlpha = data.FloatValue * OffIntensity;
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
                    var d = chromaLiteData.Find(d => d.Base == data);
                    chromaLiteData.Remove(d);
                    UpdateExistingWithChromaLite(original.SongBpmTime);
                    return;
                }
        }

        if (original.CustomLightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            var d = chromaGradientData.Find(d => d.Base == data);
            chromaGradientData.Remove(d);
            UpdateExistingWithChromaGradient(
                original.SongBpmTime,
                original.SongBpmTime + original.CustomLightGradient.Duration);
        }

        var affectedLights = original.CustomLightID != null && Settings.Instance.EmulateChromaAdvanced
            ? GetLightControllerFromLightIds(original)
            : lightIDToController.Values.AsEnumerable();

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
                    previousStateData.EndAlpha = previousStateData.Base.FloatValue * OffIntensity;
            }

            if (nextStateData.Base.IsOff) nextStateData.StartColor = previousStateData.EndColor;
        }

        InsertWithChromaGradient(previousStateData);
        InsertWithChromaGradient(nextStateData);
    }

    private static LightColor InferColorFromEvent(BaseEvent evt) =>
        evt.IsBlue ? LightColor.Blue : evt.IsRed ? LightColor.Red : LightColor.White;

    private static bool IsValidEventToTransition(BaseEvent evt) => evt.IsOn || evt.IsOff || evt.IsTransition;

    private IEnumerable<LightController> GetLightControllerFromLightIds(BaseEvent data)
    {
        var set = new HashSet<int>();
        for (var i = 0; i < data.CustomLightID.Length; i++)
        {
            var lightID = data.CustomLightID[i];
            var newId = lightIdRemap.GetValueOrDefault(lightID, lightID);
            if (!set.Add(newId)) continue;
            if (!lightIDToController.TryGetValue(newId, out var controller)) continue;
            yield return controller;
        }
    }

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

public class BasicLightStateData : BasicEventStateData
{
    public float
        StartTimeColor = float.MinValue; // this is supposedly the same as start time, special case for chroma gradient

    public LightColor StartColor;
    public Color? StartChromaColor;
    public float StartAlpha;

    public float EndTimeAlpha; // similarly this match next start, otherwise used to interpolate flash/fade
    public float EndTimeColor; // also same case above, only special case for chroma gradient
    public LightColor EndColor;
    public Color? EndChromaColor;
    public float EndAlpha;

    public Func<float, float> Easing = global::Easing.Linear;
    public bool UseHSV;

    public BasicLightStateData(BaseEvent evt) : base(evt) { }
}
