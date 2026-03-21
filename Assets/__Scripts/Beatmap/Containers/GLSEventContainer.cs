using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class GLSEventContainer : ObjectContainer
    {
        private static readonly int shaderIdColor = Shader.PropertyToID("_Color");
        private static readonly int shaderIdColorTint = Shader.PropertyToID("_ColorTint");
        private static readonly int position = Shader.PropertyToID("_Position");
        private static readonly int mainAlpha = Shader.PropertyToID("_MainAlpha");
        private static readonly int fadeSize = Shader.PropertyToID("_FadeSize");
        private static readonly int spotlightSize = Shader.PropertyToID("_SpotlightSize");

        [SerializeField] public VisualModelController VModelController;
        [SerializeField] private GLSEventAppearanceSO glsEventAppearance;
        [SerializeField] private TracksManager tracksManager;
        [SerializeField] private TextMeshPro valueDisplay;
        [SerializeField] private LightGradientController lightGradientController;
        [SerializeField] public TracksDefinitionSO TracksDefinition;

        public BaseEventBoxGroup EventBoxGroupData;

        public bool useBlockModel;
        private float oldAlpha = -1;

        public override BaseObject ObjectData
        {
            get => EventBoxGroupData;
            set => EventBoxGroupData = (BaseEventBoxGroup)value;
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

        public static GLSEventContainer SpawnGLSGroup(
            BaseEventBoxGroup data,
            TracksDefinitionSO tracksDefinition,
            ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<GLSEventContainer>();
            container.EventBoxGroupData = data;
            container.TracksDefinition = tracksDefinition;
            container.transform.localEulerAngles = Vector3.zero;
            return container;
        }

        public override void UpdateGridPosition()
        {
            transform.localPosition = new Vector3(
                GetPositionFromTrackDefinition() + 0.5f,
                0.5f,
                EventBoxGroupData.SongBpmTime * EditorScaleController.EditorScale
            );
            UpdateCollisionGroups();
        }

        private float GetPositionFromTrackDefinition()
        {
            var track = TracksDefinition.GetGlsOrDefault(EventBoxGroupData.ID);

            var offset = 0f;
            if (track.ColorTrack)
            {
                if (EventBoxGroupData is BaseLightColorEventBoxGroup) return offset;
                offset++;
            }

            if (track.RotationTracks.Any(x => x))
            {
                if (EventBoxGroupData is BaseLightRotationEventBoxGroup) return offset;
                offset++;
            }

            if (track.TranslationTracks.Any(x => x))
            {
                if (EventBoxGroupData is BaseLightTranslationEventBoxGroup) return offset;
                offset++;
            }

            if (track.FloatFXTrack && EventBoxGroupData is BaseVfxEventEventBoxGroup) return offset;

            return -1f;
        }

        public void UpdateOffset(Vector3 offset, bool updateMaterials = true)
        {
            MpbController.Mpb.SetVector(position, offset);
            if (updateMaterials) UpdateMaterials();
        }
    }
}
