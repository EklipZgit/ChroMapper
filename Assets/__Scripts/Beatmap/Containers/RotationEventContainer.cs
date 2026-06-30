using Beatmap.Base;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class RotationEventContainer : ObjectContainer
    {
        [SerializeField] public VisualModelController VModelController;
        [SerializeField] private TracksManager tracksManager;
        [SerializeField] private TextMeshProUGUI valueDisplay;

        public BaseRotationEvent EventData;

        public override BaseObject ObjectData
        {
            get => EventData;
            set => EventData = (BaseRotationEvent)value;
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
            var vm = VisualSettings.GetBlockModel();
            VModelController.Set(vm);
        }

        public static RotationEventContainer SpawnEvent(BaseRotationEvent data, ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<RotationEventContainer>();
            container.EventData = data;
            container.transform.localEulerAngles = Vector3.zero;
            return container;
        }

        public override void UpdateGridPosition()
        {
            var gridPos = EventData.GetPosition();

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
            UpdateCollisionGroups();
        }

        public void UpdateScale(float scale) => transform.localScale = Vector3.one * scale;

        public void UpdateTextDisplay(bool visible, string text = "")
        {
            if (visible != valueDisplay.gameObject.activeSelf) valueDisplay.gameObject.SetActive(visible);
            valueDisplay.text = text;
        }
    }
}
