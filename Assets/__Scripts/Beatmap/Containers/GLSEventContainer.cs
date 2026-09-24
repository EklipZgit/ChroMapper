using System;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class GLSEventContainer : ObjectContainer
    {
        [SerializeField] public VisualModelController VModelController;
        [SerializeField] private GLSEventAppearanceSO glsEventAppearance;
        [SerializeField] private TracksManager tracksManager;
        [SerializeField] private TextMeshPro[] valueDisplays;
        [SerializeField] private GLSEventIconView iconView;
        [SerializeField] private LightGradientController lightGradientController;
        [SerializeField] public TrackDefinitionsSO TrackDefinitions;

        public BaseGLSEvent EventData;
        public int DisplayLaneIndex { private get; set; } = -1;

        // Expose the existing serialized ribbon renderer for color-transition appearance updates.
        public LightGradientController LightGradientController => lightGradientController;
        [SerializeField] private LightGradientController incomingLightGradientController;
        public LightGradientController IncomingLightGradientController => incomingLightGradientController;

        public override BaseObject ObjectData { get => EventData; set => EventData = (BaseGLSEvent)value; }

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

        private void HandleModelChanged() => VModelController.Set(VisualSettings.GetBlockModel());

        public override void Setup() => DisablePassedObjectDither();

        public static GLSEventContainer SpawnGLSEvent(
            BaseGLSEvent data,
            TrackDefinitionsSO trackDefinitions,
            ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<GLSEventContainer>();
            container.EventData = data;
            // PR 666 renamed the track-definition API; retain the GLS incoming-ribbon clone on the renamed model.
            container.TrackDefinitions = trackDefinitions;
            container.incomingLightGradientController = Instantiate(
                container.lightGradientController, container.lightGradientController.transform.parent);
            container.incomingLightGradientController.name = "Incoming Color Transition Ribbon";
            container.incomingLightGradientController.gameObject.SetActive(false);
            return container;
        }

        public override void UpdateGridPosition()
        {
            var laneIndex = DisplayLaneIndex >= 0
                ? DisplayLaneIndex
                : EventData.BoxIndex;
            transform.localPosition = new Vector3(
                0.5f + laneIndex,
                BeatmapConstant.EventNodeGroundedCenterY,
                EventData.SongBpmTime * EditorScaleController.EditorScale);
            UpdateCollisionGroups();
        }

        public void SetText(bool enable)
        {
            foreach (var textMeshPro in valueDisplays) textMeshPro.enabled = enable;
        }

        public void SetText(string text)
        {
            foreach (var textMeshPro in valueDisplays) textMeshPro.SetText(text);
        }

        public void SetIcons(GLSEventIconState state)
        {
            iconView.SetIcons(state, EventData, valueDisplays);
        }

        public void SetColorHover(bool visible)
        {
            if (iconView == null)
            {
                return;
            }

            iconView.SetColorHover(visible, valueDisplays);
        }
    }
}
