using Beatmap.Base;
using Beatmap.Base.Customs;
using UnityEngine;

namespace Beatmap.Containers
{
    public class CustomEventContainer : ObjectContainer
    {
        [SerializeField] public VisualModelController ModelController;
        [SerializeField] private GameObject logo;

        private CustomEventGridContainer collection;
        public BaseCustomEvent CustomEventData;

        public override BaseObject ObjectData
        {
            get => CustomEventData;
            set => CustomEventData = (BaseCustomEvent)value;
        }

        protected override void RegisterCallback() =>
            VisualSettings.OnCustomEventModelChanged += HandleModelChanged;

        protected override void UnregisterCallback() =>
            VisualSettings.OnCustomEventModelChanged -= HandleModelChanged;

        public override void Setup()
        {
            base.Setup();
            HandleModelChanged();
        }

        private void HandleModelChanged()
        {
            var model = VisualSettings.GetCustomEventModel();
            ModelController.Set(model);
            logo.SetActive(model == VisualSettings.GetBlockModel());
        }

        public static CustomEventContainer SpawnCustomEvent(
            BaseCustomEvent data,
            CustomEventGridContainer collection,
            ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<CustomEventContainer>();
            container.CustomEventData = data;
            container.collection = collection;
            return container;
        }

        public override void UpdateGridPosition()
        {
            transform.localPosition = new Vector3(
                collection.GetLaneIndex(CustomEventData) + 0.5f,
                0.5f,
                CustomEventData.SongBpmTime * EditorScaleController.EditorScale);
            UpdateCollisionGroups();
        }
    }
}
