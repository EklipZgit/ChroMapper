using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class GLSGroupContainer : ObjectContainer
    {
        [SerializeField] public VisualModelController VModelController;
        [SerializeField] private GLSGroupAppearanceSO glsGroupAppearance;
        [SerializeField] private TracksManager tracksManager;
        [SerializeField] private TextMeshPro[] valueDisplays;
        [SerializeField] private LightGradientController lightGradientController;
        [SerializeField] public TrackDefinitionsSO TrackDefinitions;

        public BaseEventBoxGroup EventBoxGroupData;

        public override BaseObject ObjectData
        {
            get => EventBoxGroupData;
            set => EventBoxGroupData = (BaseEventBoxGroup)value;
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

        private void HandleModelChanged() => VModelController.Set(VisualSettings.GetBlockModel());

        public static GLSGroupContainer SpawnGLSGroup(
            BaseEventBoxGroup data,
            TrackDefinitionsSO trackDefinitions,
            ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<GLSGroupContainer>();
            container.EventBoxGroupData = data;
            container.TrackDefinitions = trackDefinitions;
            return container;
        }

        public override void UpdateGridPosition()
        {
            var pos = transform.localPosition;
            pos.z = EventBoxGroupData.SongBpmTime * EditorScaleController.EditorScale;
            transform.localPosition = pos;
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

        public static float GetPositionFromTrackDefinition(TrackDefinitionsSO trackDefinitions, BaseEventBoxGroup data)
        {
            var track = trackDefinitions.GetGlsOrDefault(data.ID);

            var offset = 0f;
            if (track.ColorTrack)
            {
                if (data is BaseLightColorEventBoxGroup) return offset;
                offset++;
            }

            if (track.RotationTracks.Any(x => x))
            {
                if (data is BaseLightRotationEventBoxGroup) return offset;
                offset++;
            }

            if (track.TranslationTracks.Any(x => x))
            {
                if (data is BaseLightTranslationEventBoxGroup) return offset;
                offset++;
            }

            if (track.FloatFXTrack && data is BaseVfxEventEventBoxGroup) return offset;

            return -1f;
        }
    }
}
