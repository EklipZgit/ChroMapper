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
        private static readonly int shaderIdColor = Shader.PropertyToID("_Color");
        private static readonly int shaderIdColorTint = Shader.PropertyToID("_ColorTint");
        private static readonly int position = Shader.PropertyToID("_Position");
        private static readonly int mainAlpha = Shader.PropertyToID("_MainAlpha");
        private static readonly int fadeSize = Shader.PropertyToID("_FadeSize");
        private static readonly int spotlightSize = Shader.PropertyToID("_SpotlightSize");

        [SerializeField] public VisualModelController VModelController;
        [SerializeField] private EventGridContainer EventGridContainer;
        [SerializeField] private EventAppearanceSO eventAppearance;
        [SerializeField] private TracksManager tracksManager;
        [SerializeField] private TextMeshPro valueDisplay;
        [SerializeField] private LightGradientController lightGradientController;
        [SerializeField] private CreateEventTypeLabels labels;
        [SerializeField] public TracksDefinitionSO TracksDefinition;

        public BaseEvent EventData;

        public bool useBlockModel;
        private float oldAlpha = -1;

        public static Vector3 FlashShaderOffset => new(0f, 0f, 1.2f);
        public static Vector3 FadeShaderOffset => new(0f, 0f, -1.2f);
        public static float DefaultFadeSize => 0.35f;
        public static float BoostEventFadeSize => 0.1f;

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
            VModelController.Set(useBlockModel ? VisualSettings.GetBlockModel() : VisualSettings.GetEventModel());

        public static EventContainer SpawnEvent(
            EventGridContainer eventsContainer,
            BaseEvent data,
            TracksDefinitionSO tracksDefinitionSo,
            ref GameObject prefab,
            ref EventAppearanceSO eventAppearanceSO,
            ref CreateEventTypeLabels labels)
        {
            var container = Instantiate(prefab).GetComponent<EventContainer>();
            container.EventData = data;
            container.EventGridContainer = eventsContainer;
            container.eventAppearance = eventAppearanceSO;
            container.TracksDefinition = tracksDefinitionSo;
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
                transform.localPosition = new Vector3(
                    transform.localPosition.x,
                    transform.localPosition.y + ((GetHeight() - 1f) / 2.775f),
                    transform.localPosition.z);
            UpdateCollisionGroups();
        }

        public void ChangeColor(Color c, bool updateMaterials = true)
        {
            MpbController.Mpb.SetColor(shaderIdColorTint, c);
            if (updateMaterials) UpdateMaterials();
        }

        public void ChangeBaseColor(Color c, bool updateMaterials = true)
        {
            MpbController.Mpb.SetColor(shaderIdColor, c);
            if (updateMaterials) UpdateMaterials();
        }

        public void ChangeFadeSize(float size, bool updateMaterials = true)
        {
            MpbController.Mpb.SetFloat(fadeSize, size);
            if (updateMaterials) UpdateMaterials();
        }

        public void ChangeSpotlightSize(float size, bool updateMaterials = true)
        {
            MpbController.Mpb.SetFloat(spotlightSize, size);
            if (updateMaterials) UpdateMaterials();
        }

        public void UpdateOffset(Vector3 offset, bool updateMaterials = true)
        {
            MpbController.Mpb.SetVector(position, offset);
            if (updateMaterials) UpdateMaterials();
        }

        public void UpdateAlpha(float alpha, bool updateMaterials = true)
        {
            var oldAlphaTemp = MpbController.Mpb.GetFloat(mainAlpha);
            if (oldAlphaTemp > 0) oldAlpha = oldAlphaTemp;
            if (oldAlpha == alpha) return;

            MpbController.Mpb.SetFloat(mainAlpha, alpha == -1 ? oldAlpha : alpha);
            if (updateMaterials) UpdateMaterials();
        }

        public void UpdateScale(float scale) =>
            transform.localScale =
                new Vector3(1, Settings.Instance.VisualizeChromaAlpha ? GetHeight() : 1, 1) * scale;

        //you can do this instead//Change the scale of the event height based on the alpha of the event if alpha visualization is on
        private float GetHeight()
        {
            // Non-light events should not have different heights
            if (TracksDefinition.GetBasicOrDefault(EventData.Type).Kind != BasicEventKind.Lights) return 1f;

            var height = EventData.FloatValue;
            if (EventData.CustomColor != null && Math.Abs(EventData.CustomColor.Value.a - 1) > 0.001)
            {
                height *= EventData.CustomColor.Value.a;
            }
            else if (EventData.CustomLightGradient != null
                && Math.Abs(EventData.CustomLightGradient.StartColor.a - 1) > 0.001)
            {
                height *= EventData.CustomLightGradient.StartColor.a;
            }

            // Clamped to avoid too small/too tall events
            return Mathf.Clamp(height, 0.1f, 1.5f);
        }

        public void UpdateGradientRendering(
            Color? startColor = null,
            Color? endColor = null,
            string easing = "easeLinear")
        {
            if (TracksDefinition.GetBasicOrDefault(EventData.Type).Kind != BasicEventKind.Lights)
            {
                lightGradientController.SetVisible(false);
                return;
            }

            if (EventData.CustomLightGradient != null)
            {
                if (Settings.Instance.EmulateChromaLite && EventData.Value != (int)LightValue.Off)
                {
                    ChangeColor(EventData.CustomLightGradient.StartColor);
                    ChangeBaseColor(EventData.CustomLightGradient.StartColor);
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

        public void RefreshAppearance() => eventAppearance.SetEventAppearance(this, TracksDefinition);
    }
}
