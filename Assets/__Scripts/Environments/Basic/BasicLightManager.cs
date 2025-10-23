using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.Serialization;

public class BasicLightManager : BasicEventManager<BasicLightStateData>
{
    public static PlatformColorScheme ColorScheme;
    private static bool useBoost;

    [FormerlySerializedAs("disableCustomInitialization")]
    public bool DisableCustomInitialization;

    public static readonly float FadeTimeSecond = 1.2f;
    public static readonly float FlashTimeSecond = 0.5f;
    public static float FadeTimeBeat = FadeTimeSecond;
    public static float FlashTimeBeat = FlashTimeSecond;
    public static readonly float HDRIntensity = Mathf.GammaToLinearSpace(2.4169f);

    public float GroupingMultiplier = 1.0f;
    public float GroupingOffset = 0.001f;

    public List<LightingObject> ControllingLights = new();
    public LightGroup[] LightsGroupedByZ = { };

    public List<RotatingLightsManagerBase> RotatingLights = new();

    public Dictionary<int, int> LightIDPlacementMap;
    public Dictionary<int, int> LightIDPlacementMapReverse;
    public Dictionary<int, LightingObject> LightIDMap;

    private readonly Dictionary<LightingObject, BasicEventStateChunksContainer<BasicLightStateData>>
        stateChunksContainerMap =
            new();

    private List<ChromaLiteData> chromaLiteDatas = new();
    private List<ChromaGradientData> chromaGradientDatas = new();

    private void Start() => LoadOldLightOrder();

    public void LoadOldLightOrder()
    {
        if (!DisableCustomInitialization)
        {
            foreach (var e in GetComponentsInChildren<LightingObject>())
                // No, stop that. Enforcing Light ID breaks Glass Desert
            {
                if (!e.OverrideLightGroup) ControllingLights.Add(e);
            }

            foreach (var e in GetComponentsInChildren<RotatingLightsManagerBase>())
            {
                if (!e.IsOverrideLightGroup()) RotatingLights.Add(e);
            }

            var lightIdOrder = ControllingLights
                .OrderBy(x => x.LightID)
                .GroupBy(x => x.LightID)
                .Select(x => x.First())
                .ToList();
            LightIDPlacementMap = lightIdOrder.ToDictionary(x => lightIdOrder.IndexOf(x), x => x.LightID);
            LightIDPlacementMapReverse = lightIdOrder.ToDictionary(x => x.LightID, x => lightIdOrder.IndexOf(x));
            LightIDMap = lightIdOrder.ToDictionary(x => x.LightID, x => x);

            LightsGroupedByZ = GroupLightsBasedOnZ();
            RotatingLights = RotatingLights.OrderBy(x => x.transform.localPosition.z).ToList();
        }
    }

    public LightGroup[] GroupLightsBasedOnZ() =>
        ControllingLights
            .Where(x => x.gameObject.activeInHierarchy)
            .Where(x => x.PropGroup >= 0)
            .GroupBy(x => Mathf.RoundToInt(x.PropGroup))
            .OrderBy(x => x.Key)
            .Select(x => new LightGroup { Lights = x.ToList() })
            .ToArray();

    public override void Initialize()
    {
        stateChunksContainerMap.Clear();
        foreach (var lightingObject in ControllingLights)
        {
            stateChunksContainerMap[lightingObject] =
                InitializeStates(new BasicEventStateChunksContainer<BasicLightStateData>());
            foreach (var state in stateChunksContainerMap[lightingObject].Chunks.SelectMany(chunk => chunk))
            {
                if (lightingObject.CanBeTurnedOff) continue;
                state.CanBeTurnedOff = false;
                state.Base.FloatValue = 1f;
                state.StartAlpha = state.EndAlpha = GetNoTurnOffAlpha(state.Base.FloatValue);
            }
        }
    }

    public override void UpdateTime(float currentTime)
    {
        foreach (var (lightingObject, container) in stateChunksContainerMap)
        {
            if (!container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying))
                UpdateObject(lightingObject, container.CurrentState);
            lightingObject.UpdateTime(currentTime);
        }
    }

    private static void UpdateObject(LightingObject lightingObject, BasicLightStateData stateData) =>
        lightingObject.UpdateFromState(stateData);

    public void ToggleBoost(bool boost)
    {
        useBoost = boost;
        foreach (var lightingObject in ControllingLights)
        {
            lightingObject.UpdateBoostState(boost);
            if (!stateChunksContainerMap.TryGetValue(lightingObject, out var container)) continue;
            lightingObject.UpdateStartAndEndColor(
                GetStartColorFromState(lightingObject, container.CurrentState),
                GetEndColorFromState(lightingObject, container.CurrentState));
        }
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

    public override void BuildFromData(IEnumerable<BaseEvent> events)
    {
        foreach (var evt in events) InsertData(evt);
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

        if (previousStateData.Base.IsOff && !previousStateData.CanBeTurnedOff)
        {
            previousStateData.StartAlpha =
                previousStateData.EndAlpha = GetNoTurnOffAlpha(previousStateData.Base.FloatValue);
        }

        if (newStateData.Base.IsOff && !newStateData.CanBeTurnedOff)
            newStateData.StartColor = previousStateData.EndColor;
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

        foreach (var enumerator in stateChunksContainerMap.Values.Select(container =>
            container.EnumerateFrom(from.Base.SongBpmTime)))
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
        foreach (var (container, enumerator) in stateChunksContainerMap.Values.Select(container =>
            (container, container.EnumerateFrom(startTime))))
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
                    if ((state.Base.CustomColor != null)
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

    public override void InsertData(BaseEvent evt)
    {
        Color? chromaColor = null;

        // Check if its a legacy Chroma RGB event
        switch (evt.Value)
        {
            case >= ColourManager.RgbintOffset when Settings.Instance.EmulateChromaLite:
                {
                    chromaLiteDatas.Add(
                        new() { Base = evt, Color = ColourManager.ColourFromInt(evt.Value) });
                    chromaLiteDatas = chromaLiteDatas.OrderBy(cl => cl.Base.SongBpmTime).ToList();
                    UpdateExistingWithChromaLite(evt.SongBpmTime);
                    return;
                }
            case ColourManager.RGBReset when Settings.Instance.EmulateChromaLite:
                {
                    chromaLiteDatas.Add(new() { Base = evt, Color = null });
                    chromaLiteDatas = chromaLiteDatas.OrderBy(cl => cl.Base.SongBpmTime).ToList();
                    UpdateExistingWithChromaLite(evt.SongBpmTime);
                    return; // this was a break, not sure why
                }
        }

        //Check if it is a PogU new Chroma event
        if ((evt.CustomColor != null) && Settings.Instance.EmulateChromaLite && !evt.IsWhite) // White overrides Chroma
            chromaColor = (Color)evt.CustomColor;

        if (chromaLiteDatas.Count > 0)
        {
            var chromaLiteIndex = chromaLiteDatas.FindLastIndex(data => data.Base.SongBpmTime <= evt.SongBpmTime);
            if (chromaLiteIndex != -1 && Settings.Instance.EmulateChromaLite)
                chromaColor = chromaLiteDatas[chromaLiteIndex].Color;
        }

        if (evt.CustomLightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            chromaGradientDatas.Add(
                new ChromaGradientData
                {
                    Base = evt,
                    StartTime = evt.SongBpmTime,
                    EndTime =
                        evt.SongBpmTime
                        + evt.CustomLightGradient.Duration, // TODO: duration is not actual song bpm time
                    StartColor = evt.CustomLightGradient.StartColor,
                    EndColor = evt.CustomLightGradient.EndColor,
                    Easing = Easing.Named(evt.CustomLightGradient.EasingType)
                });
            chromaGradientDatas = chromaGradientDatas.OrderBy(cl => cl.StartTime).ToList();
            UpdateExistingWithChromaGradient(evt.SongBpmTime, evt.SongBpmTime + evt.CustomLightGradient.Duration);
        }

        //Check to see if we're soloing any particular event
        // wtf is solo event
        // if (SoloAnEventType && evt.Type != SoloEventType) mainColor = invertedColor = Color.black.WithAlpha(0);

        var affectedLights = ControllingLights;
        if (evt.CustomLightID != null && LightIDMap != null && Settings.Instance.EmulateChromaAdvanced)
        {
            var lightIDArr = evt.CustomLightID;
            var filteredLights = new List<LightingObject>(lightIDArr.Length);
            foreach (var lightID in lightIDArr)
            {
                if (!LightIDMap.TryGetValue(lightID, out var lightingObject)) continue;
                filteredLights.Add(lightingObject);
            }

            affectedLights = filteredLights;
        }

        foreach (var lightingObject in affectedLights)
        {
            var newState = CreateState(evt);
            newState.StartTime = evt.SongBpmTime;
            newState.StartTimeColor = evt.SongBpmTime;
            newState.StartColor = InferColorFromEvent(evt);
            newState.StartChromaColor = chromaColor;
            newState.StartAlpha = evt.FloatValue;
            newState.EndTime = float.MaxValue;
            newState.EndTimeAlpha = float.MaxValue;
            newState.EndTimeColor = float.MaxValue;
            newState.EndColor = InferColorFromEvent(evt);
            newState.EndChromaColor = chromaColor;
            newState.EndAlpha = evt.FloatValue;
            newState.CanBeTurnedOff = lightingObject.CanBeTurnedOff;

            if (evt.IsOff)
            {
                if (lightingObject.CanBeTurnedOff)
                    newState.StartAlpha = newState.EndAlpha = 0f;
                else
                    newState.StartAlpha = newState.EndAlpha = GetNoTurnOffAlpha(evt.FloatValue);
            }
            else if (evt.IsFlash)
            {
                newState.EndTimeAlpha = newState.StartTime + FlashTimeBeat;
                newState.StartAlpha = evt.FloatValue * 1.2f;
                newState.EndAlpha = evt.FloatValue;
                newState.Easing = Easing.Cubic.Out;
            }
            else if (evt.IsFade)
            {
                newState.EndTimeAlpha = newState.StartTime + FadeTimeBeat;
                newState.StartAlpha = evt.FloatValue * 1.2f;
                newState.EndAlpha = 0f;
                newState.Easing = Easing.Exponential.Out;
                if (!lightingObject.CanBeTurnedOff) newState.EndAlpha = GetNoTurnOffAlpha(evt.FloatValue);
            }

            InsertWithChromaGradient(newState);

            var container = stateChunksContainerMap[lightingObject];

            // let's assume this will be previous state if this is inserted within the range
            var previousState = container.CurrentState;
            var previousValid = previousState.IsWithinRange(evt.SongBpmTime);
            HandleInsertState(container, newState);

            if (!previousValid) continue;
            container.SetStateAt(Atsc.CurrentSongBpmTime);
            UpdateObject(lightingObject, container.CurrentState);
        }
    }

    public override void RemoveData(BaseEvent evt, BaseEvent original)
    {
        switch (original.Value)
        {
            case >= ColourManager.RgbintOffset when Settings.Instance.EmulateChromaLite:
            case ColourManager.RGBReset when Settings.Instance.EmulateChromaLite:
                {
                    var data = chromaLiteDatas.Find(data => data.Base == evt);
                    chromaLiteDatas.Remove(data);
                    UpdateExistingWithChromaLite(original.SongBpmTime);
                    return;
                }
        }

        if (original.CustomLightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            var data = chromaGradientDatas.Find(data => data.Base == evt);
            chromaGradientDatas.Remove(data);
            UpdateExistingWithChromaGradient(
                original.SongBpmTime,
                original.SongBpmTime + original.CustomLightGradient.Duration);
        }

        IEnumerable<LightingObject> affectedLights = ControllingLights;

        if (original.CustomLightID != null && LightIDMap != null && Settings.Instance.EmulateChromaAdvanced)
        {
            var lightIDArr = original.CustomLightID;
            var filteredLights = new List<LightingObject>(lightIDArr.Length);
            foreach (var lightID in lightIDArr)
            {
                if (!LightIDMap.TryGetValue(lightID, out var lightingObject)) continue;
                filteredLights.Add(lightingObject);
            }

            affectedLights = filteredLights;
        }

        foreach (var lightingObject in affectedLights)
        {
            var container = stateChunksContainerMap[lightingObject];
            HandleRemoveState(container, evt);

            // unfortunately, we cannot do the same as insertion so we need to search
            var (_, _, previousState) = container.GetStateAt(Atsc.CurrentSongBpmTime);
            if (!previousState.IsWithinRange(evt.SongBpmTime)) continue;
            container.SetStateAt(Atsc.CurrentSongBpmTime);
            UpdateObject(lightingObject, container.CurrentState);
        }
    }

    public override void RemoveData(BaseEvent evt)
    {
        switch (evt.Value)
        {
            case >= ColourManager.RgbintOffset when Settings.Instance.EmulateChromaLite:
            case ColourManager.RGBReset when Settings.Instance.EmulateChromaLite:
                {
                    var data = chromaLiteDatas.Find(data => data.Base == evt);
                    chromaLiteDatas.Remove(data);
                    UpdateExistingWithChromaLite(evt.SongBpmTime);
                    return;
                }
        }

        if (evt.CustomLightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            var data = chromaGradientDatas.Find(data => data.Base == evt);
            chromaGradientDatas.Remove(data);
            UpdateExistingWithChromaGradient(evt.SongBpmTime, evt.SongBpmTime + evt.CustomLightGradient.Duration);
        }

        IEnumerable<LightingObject> affectedLights = ControllingLights;

        if (evt.CustomLightID != null && LightIDMap != null && Settings.Instance.EmulateChromaAdvanced)
        {
            var lightIDArr = evt.CustomLightID;
            var filteredLights = new List<LightingObject>(lightIDArr.Length);
            foreach (var lightID in lightIDArr)
            {
                if (!LightIDMap.TryGetValue(lightID, out var lightingObject)) continue;
                filteredLights.Add(lightingObject);
            }

            affectedLights = filteredLights;
        }

        foreach (var lightingObject in affectedLights)
        {
            var container = stateChunksContainerMap[lightingObject];
            HandleRemoveState(container, evt);

            // unfortunately, we cannot do the same as insertion so we need to search
            var (_, _, previousState) = container.GetStateAt(Atsc.CurrentSongBpmTime);
            if (!previousState.IsWithinRange(evt.SongBpmTime)) continue;
            container.SetStateAt(Atsc.CurrentSongBpmTime);
            UpdateObject(lightingObject, container.CurrentState);
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

            if (previousStateData.Base.IsOff && !previousStateData.CanBeTurnedOff)
            {
                previousStateData.StartAlpha =
                    previousStateData.EndAlpha = GetNoTurnOffAlpha(previousStateData.Base.FloatValue);
            }

            if (nextStateData.Base.IsOff && !nextStateData.CanBeTurnedOff)
                nextStateData.StartColor = previousStateData.EndColor;
        }

        InsertWithChromaGradient(previousStateData);
        InsertWithChromaGradient(nextStateData);
    }


    public override void Reset()
    {
        foreach (var lightingObject in stateChunksContainerMap.Keys)
            UpdateObject(lightingObject, stateChunksContainerMap[lightingObject].CurrentState);
    }

    private static LightColor InferColorFromEvent(BaseEvent evt) =>
        evt.IsBlue ? LightColor.Blue : evt.IsRed ? LightColor.Red : LightColor.White;

    public static Color GetStartColorFromState(LightingObject lightingObject, BasicLightStateData stateData) =>
        (stateData.StartChromaColor
            ?? GetColorFromScheme(
                stateData.StartColor,
                lightingObject.UseInvertedPlatformColors)); // .Multiply(HDRIntensity);

    public static Color GetEndColorFromState(LightingObject lightingObject, BasicLightStateData stateData) =>
        (stateData.EndChromaColor
            ?? GetColorFromScheme(
                stateData.EndColor,
                lightingObject.UseInvertedPlatformColors)); // .Multiply(HDRIntensity);

    private static Color GetColorFromScheme(LightColor value, bool useInvertedPlatformColors)
    {
        return value switch
        {
            LightColor.Blue when useInvertedPlatformColors => useBoost
                ? ColorScheme.RedBoostColor
                : ColorScheme.RedColor,
            LightColor.Blue => useBoost ? ColorScheme.BlueBoostColor : ColorScheme.BlueColor,
            LightColor.Red when useInvertedPlatformColors => useBoost
                ? ColorScheme.BlueBoostColor
                : ColorScheme.BlueColor,
            LightColor.Red => useBoost ? ColorScheme.RedBoostColor : ColorScheme.RedColor,
            LightColor.White => useBoost ? ColorScheme.WhiteBoostColor : ColorScheme.WhiteColor,
            _ => Color.white
        };
    }

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

    private static float GetNoTurnOffAlpha(float value) => value * 2f / 3f;

    [Serializable]
    public class LightGroup
    {
        public List<LightingObject> Lights = new();
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
    public bool CanBeTurnedOff = true;

    public BasicLightStateData(BaseEvent evt) : base(evt) { }
}
