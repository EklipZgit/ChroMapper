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
        [SerializeField] private LightGradientController lightGradientController;
        [SerializeField] public TracksDefinitionSO TracksDefinition;

        public BaseGLSEvent EventData;

        // Expose the existing serialized ribbon renderer for color-transition appearance updates.
        public LightGradientController LightGradientController => lightGradientController;

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
            return container;
        }

        public override void UpdateGridPosition()
        {
            transform.localPosition = new Vector3(
                0.5f + EventData.BoxIndex,
                // Keep every inner GLS node grounded after its shared 75%-scale appearance is applied. Fixes GLS nodes hovering too high above grid and being hard to tell where they are visually.
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
    }
}
