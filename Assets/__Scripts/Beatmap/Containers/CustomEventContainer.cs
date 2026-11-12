using Beatmap.Base;
using Beatmap.Base.Customs;
using UnityEngine;

namespace Beatmap.Containers
{
    public class CustomEventContainer : ObjectContainer
    {
        private const float LogoSurfaceOffset = 0.01f;
        private const float PyramidLogoCoverage = 0.94f;
        private static readonly Vector3 blockLogoPosition = new(0, 0.5f + LogoSurfaceOffset, 0);
        private static readonly Quaternion blockLogoRotation = Quaternion.Euler(90, 0, 0);
        private static readonly Vector3 blockLogoScale = Vector3.one * 0.1f;
        // ChroMapper_NewTransparent's visible artwork is 512x366 pixels at 100 pixels per unit.
        private static readonly Vector2 logoArtworkSize = new(5.12f, 3.66f);

        [SerializeField] public VisualModelController ModelController;
        [SerializeField] private GameObject logo;
        [SerializeField] private Material bodyMaterial;
        [SerializeField] private VisualModelSO pyramidModel;
        [SerializeField] private VisualModelSO flatPyramidModel;

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
            foreach (var renderer in ModelController.Renderers)
                renderer.sharedMaterial = bodyMaterial;

            var hasFlatBack = model == pyramidModel || model == flatPyramidModel;
            logo.SetActive(model == VisualSettings.GetBlockModel() || hasFlatBack);
            if (hasFlatBack)
            {
                var bounds = model.Collider.bounds;
                logo.transform.localScale = new Vector3(
                    bounds.size.x * PyramidLogoCoverage / logoArtworkSize.x,
                    bounds.size.y * PyramidLogoCoverage / logoArtworkSize.y,
                    blockLogoScale.z);
                // The sprite faces local -Z, toward the camera and away from the pyramid's flat back.
                logo.transform.SetLocalPositionAndRotation(
                    new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - LogoSurfaceOffset),
                    Quaternion.identity);
            }
            else
            {
                logo.transform.localScale = blockLogoScale;
                logo.transform.SetLocalPositionAndRotation(
                    blockLogoPosition,
                    blockLogoRotation);
            }
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
