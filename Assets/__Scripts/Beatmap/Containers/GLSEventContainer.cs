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
        // A dedicated prefab view keeps OE-style sprite layout independent from GLS block shaders and event formatting.
        [SerializeField] private GLSEventIconView iconView;
        [SerializeField] private LightGradientController lightGradientController;
        [SerializeField] public TracksDefinitionSO TracksDefinition;

        public BaseGLSEvent EventData;
        public int DisplayLaneIndex { private get; set; } = -1;

        // Expose the existing serialized ribbon renderer for color-transition appearance updates.
        public LightGradientController LightGradientController => lightGradientController;
        // First nodes need a distinct incoming cross-group ribbon while retaining their normal outgoing interval.
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

        public static GLSEventContainer SpawnGLSEvent(
            BaseGLSEvent data,
            TracksDefinitionSO tracksDefinition,
            ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<GLSEventContainer>();
            container.EventData = data;
            container.TracksDefinition = tracksDefinition;
            // InnerFirstNodeHasIncomingRibbonFromA establishes both renderer dependencies at spawn, never during hover or refresh.
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

        // Appearance refreshes update icon identity and event-specific layout together so rapid edits cannot leave a stale face.
        public void SetIcons(GLSEventIconState state)
        {
            // Reuse the container's established TMP dependency so domain reload cannot leave a duplicate serialized array empty.
            iconView.SetIcons(state, EventData, valueDisplays);
        }
    }
}
