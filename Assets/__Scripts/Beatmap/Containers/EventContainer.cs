using System;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
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

        [SerializeField] public VisualModelController VModelController;
        [SerializeField] private EventGridContainer EventGridContainer;
        [SerializeField] private EventAppearanceSO eventAppearance;
        [SerializeField] private TracksManager tracksManager;
        [SerializeField] private TextMeshPro valueDisplay;
        [SerializeField] private LightGradientController lightGradientController;
        [SerializeField] private CreateEventTypeLabels labels;
        [SerializeField] public TrackDefinitionsSO TrackDefinitions;

        public BaseEvent EventData;

        public bool useBlockModel;
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

        private void HandleModelChanged() =>
            VModelController.Set(useBlockModel ? VisualSettings.GetEventBlockModel() : VisualSettings.GetEventModel());

        public static EventContainer SpawnEvent(
            EventGridContainer eventsContainer,
            BaseEvent data,
            TrackDefinitionsSO trackDefinitionsSo,
            ref GameObject prefab,
            ref CreateEventTypeLabels labels)
        {
            var container = Instantiate(prefab).GetComponent<EventContainer>();
            container.EventData = data;
            container.EventGridContainer = eventsContainer;
            container.TrackDefinitions = trackDefinitionsSo;
            container.labels = labels;
            container.transform.localEulerAngles = Vector3.zero;
            return container;
        }

        public override void UpdateGridPosition()
        {
            var gridPos = EventData.GetPosition(
                labels,
                EventGridContainer.PropagationEditing,
                EventGridContainer.EventTypeToPropagate);

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

        public void UpdateScale(float scale) =>
            transform.localScale =
                new Vector3(1, Settings.Instance.VisualizeChromaAlpha ? GetHeight() : 1, 1) * scale;

        //you can do this instead//Change the scale of the event height based on the alpha of the event if alpha visualization is on
        private float GetHeight()
        {
            // Non-light events should not have different heights
            if (TrackDefinitions.GetBasicOrDefault(EventData.Type).Kind != BasicEventKind.Lights) return 1f;

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
            string easing = "easeLinear")
        {
            if (TrackDefinitions.GetBasicOrDefault(EventData.Type).Kind != BasicEventKind.Lights)
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
                    EventData.Next.SongBpmTime - EventData.SongBpmTime,
                    easing);
                lightGradientController.SetVisible(true);
                lightGradientController.UpdateGradientData(transition);
                lightGradientController.UpdateDuration(transition.Duration);
            }
        }

        public void UpdateTextDisplay(bool visible, string text = "")
        {
            if (visible != valueDisplay.gameObject.activeSelf) valueDisplay.gameObject.SetActive(visible);
            valueDisplay.text = text;
        }

        public void RefreshAppearance() => eventAppearance.SetAppearance(this, TrackDefinitions);
    }
}
