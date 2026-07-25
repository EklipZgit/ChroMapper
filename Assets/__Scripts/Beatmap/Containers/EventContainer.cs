using System;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Constants;
using Beatmap.Enums;
using Beatmap.Shared;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class EventContainer : ObjectContainer
    {
        private static readonly int colorAId = Shader.PropertyToID("_ColorA");
        private static readonly int colorBId = Shader.PropertyToID("_ColorB");
        private static readonly int mainAlphaId = Shader.PropertyToID("_MainAlpha");
        private static readonly int fadeSizeId = Shader.PropertyToID("_FadeSize");
        private static readonly int offsetId = Shader.PropertyToID("_Offset");

        [SerializeField] public VisualModelController VModelController;
        [SerializeField] private EventGridContainer eventGridContainer;
        [SerializeField] private EventAppearanceSO eventAppearance;
        [SerializeField] private TracksManager tracksManager;
        [SerializeField] private TextMeshPro valueDisplay;
        [SerializeField] private LightGradientController lightGradientController;
        [SerializeField] private CreateEventTypeLabels labels;
        [SerializeField] public TracksDefinitionSO TracksDefinition;

        public BaseEvent EventData;

        private void Awake() => defaultValueDisplayFontSize = valueDisplay.fontSize;

        private bool useBlockModel;
        private float defaultValueDisplayFontSize;
        public bool AlternateShader;
        private float oldAlpha = -1;

        public override BaseObject ObjectData
        {
            get => EventData;
            set => EventData = (BaseEvent)value;
        }

        public bool UseBlockModel
        {
            get => useBlockModel;
            set
            {
                useBlockModel = value;
                HandleModelChanged();
            }
        }

        protected override void RegisterCallback()
        {
            VisualSettings.OnBlockModelChanged += HandleModelChanged;
            VisualSettings.OnEventModelChanged += HandleModelChanged;
        }

        protected override void UnregisterCallback()
        {
            VisualSettings.OnBlockModelChanged -= HandleModelChanged;
            VisualSettings.OnEventModelChanged -= HandleModelChanged;
        }

        private void HandleModelChanged()
        {
            var vm = useBlockModel ? VisualSettings.GetEventBlockModel() : VisualSettings.GetEventModel();
            VModelController.Set(vm);
            
            if (AlternateShader == vm.AlternateShader) return;
            AlternateShader = vm.AlternateShader;
            if (EventData != null) RefreshAppearance();
        }

        public static EventContainer SpawnEvent(
            EventGridContainer eventsContainer,
            BaseEvent data,
            TracksDefinitionSO tracksDefinitionSo,
            ref GameObject prefab,
            ref CreateEventTypeLabels labels)
        {
            var container = Instantiate(prefab).GetComponent<EventContainer>();
            container.EventData = data;
            container.eventGridContainer = eventsContainer;
            container.TracksDefinition = tracksDefinitionSo;
            container.labels = labels;
            container.transform.localEulerAngles = Vector3.zero;
            return container;
        }

        public override void UpdateGridPosition()
        {
            var gridPos = EventData.GetPosition(
                labels,
                eventGridContainer.PropagationEditing,
                eventGridContainer.EventTypeToPropagate);

            if (gridPos == null)
            {
                transform.localPosition = new Vector3(
                    0.5f,
                    0.5f,
                    EventData.SongBpmTime * EditorScaleController.EditorScale
                );
                SafeSetActive(false);
            }
            else
            {
                transform.localPosition = new Vector3(
                    gridPos.Value.x,
                    gridPos.Value.y,
                    EventData.SongBpmTime * EditorScaleController.EditorScale
                );
            }

            transform.localEulerAngles = Vector3.zero;
            if (EventData.CustomLightGradient != null && Settings.Instance.VisualizeChromaGradients)
                lightGradientController.UpdateDuration(EventData.CustomLightGradient.Duration);
            //Move event up or down enough to give a constant distance from the bottom of the event, taking the y alpha scale into account
            if (Settings.Instance.VisualizeChromaAlpha)
            {
                transform.localPosition = new Vector3(
                    transform.localPosition.x,
                    transform.localPosition.y + ((GetHeight() - 1f) / 2.775f),
                    transform.localPosition.z);
            }

            UpdateCollisionGroups();
        }

        public void ChangeColorA(Color c, bool updateMaterials = true)
        {
            MpbController.Mpb.SetColor(colorAId, c);
            if (updateMaterials) UpdateMaterials();
        }

        public void ChangeColorB(Color c, bool updateMaterials = true)
        {
            MpbController.Mpb.SetColor(colorBId, c);
            if (updateMaterials) UpdateMaterials();
        }

        public void ChangeFadeSize(float size, bool updateMaterials = true)
        {
            MpbController.Mpb.SetFloat(fadeSizeId, size);
            if (updateMaterials) UpdateMaterials();
        }

        public void UpdateAlpha(float alpha, bool updateMaterials = true)
        {
            var oldAlphaTemp = MpbController.Mpb.GetFloat(mainAlphaId);
            if (oldAlphaTemp > 0) oldAlpha = oldAlphaTemp;
            if (Mathf.Approximately(oldAlpha, alpha)) return;

            MpbController.Mpb.SetFloat(mainAlphaId, Mathf.Approximately(alpha, -1) ? oldAlpha : alpha);
            if (updateMaterials) UpdateMaterials();
        }

        public void UpdateOffset(float offset)
        {
            MpbController.Mpb.SetFloat(offsetId, offset);
            UpdateMaterials();
        }

        public void UpdateScale(float scale) =>
            transform.localScale =
                new Vector3(
                    1,
                    IsRingRotationEvent || IsRingZoomEvent || IsLaserSpeedEvent
                        || Settings.Instance.VisualizeChromaAlpha
                        ? GetHeight()
                        : 1,
                    1) * scale;

        // Ring capabilities come from the active environment rather than conventional event-type numbers.
        private bool IsRingRotationEvent =>
            TracksDefinition.GetBasicOrDefault(EventData.Type).Components.HasFlag(BasicEventComponent.RingRotation);

        private bool IsRingZoomEvent =>
            TracksDefinition.GetBasicOrDefault(EventData.Type).Components.HasFlag(BasicEventComponent.RingZoom)
            || IsSmoothStepRingZoomEvent;

        // SmoothStepRingZoom only applies to The Second's legacy ring right now.
        private bool IsSmoothStepRingZoomEvent =>
            TracksDefinition.GetBasicOrDefault(EventData.Type).Components
                .HasFlag(BasicEventComponent.SmoothStepRingZoom);

        // Basic Event light-rotation consumers use speed as their primary visual magnitude.
        private bool IsLaserSpeedEvent =>
            TracksDefinition.GetBasicOrDefault(EventData.Type).Components.HasFlag(BasicEventComponent.LightRotation);

        //you can do this instead//Change the scale of the event height based on the alpha of the event if alpha visualization is on
        private float GetHeight()
        {
            if (IsRingRotationEvent && EventData.CustomRingRotation.HasValue)
                return Mathf.Clamp(
                    Mathf.Abs(EventData.CustomRingRotation.Value) / RingEventHeightConstants.RingRotationHeightScaleDegrees,
                    1f / RingEventHeightConstants.RingRotationHeightScaleDegrees,
                    RingEventHeightConstants.RingRotationHeightMaxMultiplier);

            if (IsRingZoomEvent && (EventData.CustomStep.HasValue
                || IsSmoothStepRingZoomEvent))
            {
                // SmoothStepRingZoom only applies to The Second's ring and falls back to i when step is absent.
                var ringZoomStep = IsSmoothStepRingZoomEvent
                    ? EventData.CustomStep ?? EventData.Value
                    : EventData.CustomStep.Value;
                return Mathf.Clamp(
                    Mathf.Abs(ringZoomStep) / RingEventHeightConstants.RingZoomHeightScaleStep,
                    1f / RingEventHeightConstants.RingZoomHeightScaleStep,
                    RingEventHeightConstants.RingZoomHeightMaxMultiplier);
            }

            if (IsLaserSpeedEvent)
            {
                // Scale speed 0..40 across the minimum node height through 300%, clamping larger values visually.
                var speed = Mathf.Max(0f, EventData.CustomSpeed ?? EventData.Value);
                return Mathf.Clamp(speed / 40f * 3f, 0.1f, 3f);
            }

            // Non-light events should not have different heights
            if (TracksDefinition.GetBasicOrDefault(EventData.Type).Kind != BasicEventKind.Lights) return 1f;

            var height = EventData.FloatValue;
            if (EventData.CustomColor != null && Math.Abs(EventData.CustomColor.Value.a - 1) > 0.001)
                height *= EventData.CustomColor.Value.a;
            else if (EventData.CustomLightGradient != null
                && Math.Abs(EventData.CustomLightGradient.StartColor.a - 1) > 0.001)
                height *= EventData.CustomLightGradient.StartColor.a;

            // Clamped to avoid too small/too tall events
            return Mathf.Clamp(height, 0.1f, 1.5f);
        }

        public void UpdateGradientRendering(
            Color? startColor = null,
            Color? endColor = null,
            string easing = "easeLinear",
            bool useHsv = false,
            bool allowNonLight = false)
        {
            // Use dev's singular serialized track-definition field.
            if (!allowNonLight && TracksDefinition.GetBasicOrDefault(EventData.Type).Kind != BasicEventKind.Lights)
            {
                lightGradientController.SetVisible(false);
                return;
            }

            if (EventData.CustomLightGradient != null)
            {
                if (Settings.Instance.EmulateChromaLite && EventData.Value != (int)LightValue.Off)
                {
                    ChangeColorB(EventData.CustomLightGradient.StartColor);
                    ChangeColorA(EventData.CustomLightGradient.StartColor);
                }

                lightGradientController.SetVisible(true);
                lightGradientController.UpdateGradientData(EventData.CustomLightGradient);
                lightGradientController.UpdateDuration(EventData.CustomLightGradient.Duration);
            }
            else
            {
                if (startColor == null || endColor == null)
                {
                    lightGradientController.SetVisible(false);
                    return;
                }

                var transition = new ChromaLightGradient(
                    startColor.Value,
                    endColor.Value,
                    EventData.Next?.SongBpmTime - EventData.SongBpmTime ?? 0f,
                    easing);
                lightGradientController.SetVisible(true);
                // Basic Event transitions can explicitly interpolate through HSV instead of RGB.
                lightGradientController.UpdateGradientData(transition, useHsv);
                lightGradientController.UpdateDuration(transition.Duration);
            }
        }

        public void UpdateTextDisplay(bool visible, string text = "")
        {
            if (visible != valueDisplay.gameObject.activeSelf) valueDisplay.gameObject.SetActive(visible);
            var isRotationEvent = IsRingRotationEvent
                || TracksDefinition.GetBasicOrDefault(EventData.Type).Components.HasFlag(BasicEventComponent.LightRotation);

            var lineCount = text.Split('\n').Length;
            var scaleFactor = 1f;
            if (isRotationEvent && lineCount >= 2)
            {
                scaleFactor = lineCount switch
                {
                    2 => 0.5f,
                    3 => 0.4f,
                    _ => 0.3f
                };
            }
            else if (text.Contains('\n') || isRotationEvent)
            {
                scaleFactor = 0.5f;
            }

            // Give single-line decimal speeds extra width without compounding the multiline label reduction.
            if (lineCount == 1 && IsLaserSpeedEvent && EventData.CustomSpeed.HasValue
                && !Mathf.Approximately(EventData.CustomSpeed.Value, Mathf.Round(EventData.CustomSpeed.Value)))
                scaleFactor *= 0.8f;

            valueDisplay.fontSize = defaultValueDisplayFontSize * scaleFactor;
            valueDisplay.text = text;
        }

        public bool IsOverlapping { get; private set; }

        public void RefreshAppearance()
        {
            // Attempt to highlight GLS event groups that clash with other GLS event groups.
            IsOverlapping = ComputeOverlap();
            // Refresh through the dev branch's TracksDefinition field.
            eventAppearance.SetAppearance(this, TracksDefinition);
        }

        private bool ComputeOverlap()
        {
            if (!IsRingRotationEvent && !IsRingZoomEvent) return false;
            if (EventData.Next == null) return false;
            if (eventGridContainer?.BeatmapContext?.Atsc == null) return false;

            var atsc = eventGridContainer.BeatmapContext.Atsc;
            var fdt = TimeHelper.FixedDeltaTime;
            if (fdt <= 0f) return false;

            var gapSeconds = atsc.GetSecondsFromBeat(EventData.Next.SongBpmTime) -
                             atsc.GetSecondsFromBeat(EventData.SongBpmTime);
            if (gapSeconds <= 0f) return false;

            var durationSeconds = 0f;
            var manager = eventGridContainer.BeatmapContext.Descriptor?.BasicEventEffectManager;

            if (IsRingRotationEvent)
            {
                var effect = manager?.GetEffect<TrackLaneRingsRotationEffect>(EventData.Type);
                var ringCount = effect?.Effect?.Manager?.Rings?.Count ?? 0;
                var prop = EventData.CustomProp ?? effect?.PropagationSpeed ?? 0f;
                var speed = EventData.CustomSpeed ?? effect?.FlexySpeed ?? 0f;

                if (prop > 0f) durationSeconds += ringCount / prop * fdt;
                durationSeconds += GetLerpDuration(speed, fdt);
            }
            else
            {
                var effect = manager?.GetEffect<TrackLaneRingsPositionEffect>(EventData.Type);
                var speed = EventData.CustomSpeed ?? 0f;
                durationSeconds += GetLerpDuration(speed, fdt);
            }

            var durationBeats = atsc.GetBeatFromSeconds(durationSeconds);
            return durationBeats > EventData.Next.SongBpmTime - EventData.SongBpmTime;
        }

        private static float GetLerpDuration(float speed, float fdt)
        {
            if (speed <= 0f || fdt <= 0f) return 0f;
            var t = fdt * speed;
            if (t >= 1f) return 0f;
            return Mathf.Log(0.001f) / Mathf.Log(1 - t) * fdt;
        }
    }
}
