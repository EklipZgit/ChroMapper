using Beatmap.Base;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ObstacleContainer : ObjectContainer
    {
        private static readonly int colorID = Shader.PropertyToID("_Color");
        private static readonly int worldScaleID = Shader.PropertyToID("_WorldScale");

        [SerializeField] private TracksManager manager;

        [SerializeField] private Transform coreTransform;
        [SerializeField] private Renderer coreRenderer;
        [SerializeField] private Transform outlineTransform;
        [SerializeField] private Renderer outlineRenderer;

        [SerializeField] private Material simpleObstacle;
        [SerializeField] private Material distortObstacle;

        [SerializeField] public BaseObstacle ObstacleData;
        public Vector3 ObstacleScale;

        public override BaseObject ObjectData
        {
            get => ObstacleData;
            set => ObstacleData = (BaseObstacle)value;
        }

        public bool IsRotatedByNoodleExtensions => ObstacleData.CustomWorldRotation != null;

        public static ObstacleContainer SpawnObstacle(
            BaseObstacle data,
            TracksManager manager,
            ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<ObstacleContainer>();
            container.ObstacleData = data;
            container.manager = manager;
            return container;
        }

        public void SwitchMaterial()
        {
            coreRenderer.sharedMaterial = UIMode.PreviewMode ? distortObstacle : simpleObstacle;
            UpdateMaterials();
        }

        public void SetColor(Color c)
        {
            MaterialPropertyBlock.SetColor(colorID, c);
            UpdateMaterials();
        }

        public void SetScale(Vector3 scale)
        {
            ObstacleScale = scale;

            scale.x *= 0.98f;
            var cubeOffset = scale / 2f;
            cubeOffset.x = 0f;

            coreTransform.localScale = scale - (Vector3.one * 0.01f);
            coreTransform.localPosition = cubeOffset;

            outlineTransform.localScale = scale;
            outlineTransform.localPosition = cubeOffset;

            foreach (var selectionRenderer in SelectionRenderers)
            {
                selectionRenderer.transform.localScale = scale;
                selectionRenderer.transform.localPosition = cubeOffset;
            }

            MaterialPropertyBlock.SetVector(worldScaleID, coreTransform.localScale);
            UpdateMaterials();
        }

        public float GetLength(float scale)
        {
            if (ObstacleData.CustomSize != null
                && ObstacleData.CustomSize.IsArray
                && ObstacleData.CustomSize[2].IsNumber)
                return ObstacleData.CustomSize[2];

            var length = ObstacleData.DurationSongBpm;

            //Take half jump duration into account if the setting is enabled.
            if (ObstacleData.Duration < 0 && Settings.Instance.ShowMoreAccurateFastWalls && !UIMode.AnimationMode)
                length -= length * Mathf.Abs(length / ObstacleData.Hjd);

            length *= scale;

            return float.IsFinite(length) ? length : float.Epsilon;
        }

        public Vector3 ReadSize()
        {
            var bounds = ObstacleData.GetShape();

            return new Vector3(
                Mathf.Abs(bounds.Width),
                Mathf.Abs(bounds.Height),
                0f
            );
        }

        public Vector3 ReadPosition()
        {
            var bounds = ObstacleData.GetShape();

            return new Vector3(
                bounds.Position + (bounds.Width / 2.0f),
                bounds.StartHeight + (bounds.Height < 0f ? bounds.Height : 0f),
                0f
            );
        }

        public void UpdateScaleWithLength(float length)
        {
            var size = ReadSize();
            size.z = length;
            SetScale(size);
        }

        public override void UpdateScalable(float scale)
        {
            var length = Mathf.Abs(GetLength(scale));
            var size = ObstacleScale;
            size.z = length;
            SetScale(size);
        }

        public override void UpdateGridPosition()
        {
            var localRotation = Vector3.zero;
            var length = GetLength(
                UIMode.AnimationMode
                    ? ObstacleData.EditorScale
                    : EditorScaleController.EditorScale);
            var position = ReadPosition();

            if (ObstacleData.CustomLocalRotation != null)
                localRotation = ObstacleData.CustomLocalRotation.ReadVector3();
            if (ObstacleData.CustomWorldRotation != null && !Animator.AnimatedTrack)
            {
                if (ObstacleData.CustomWorldRotation.IsNumber)
                    manager.CreateTrack(new Vector3(0, ObstacleData.CustomWorldRotation, 0)).AttachContainer(this);
                else
                    manager.CreateTrack(ObstacleData.CustomWorldRotation.ReadVector3()).AttachContainer(this);
            }

            // Enforce positive scale, offset our obstacles to match.
            transform.localPosition = new Vector3(
                0f,
                -0.5f,
                (ObstacleData.SongBpmTime * EditorScaleController.EditorScale) + (length < 0f ? length : 0f));
            Animator.LocalTarget.localPosition = position;

            UpdateScaleWithLength(length);

            if (localRotation != Vector3.zero)
            {
                Animator.LocalTarget.localEulerAngles = localRotation;
            }

            UpdateCollisionGroups();
        }
    }
}
