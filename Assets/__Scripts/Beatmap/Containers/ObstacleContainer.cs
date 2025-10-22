using Beatmap.Base;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ObstacleContainer : ObjectContainer
    {
        private static readonly int colorID = Shader.PropertyToID("_Color");
        private static readonly int worldScaleID = Shader.PropertyToID("_WorldScale");

        [SerializeField] private TracksManager manager;

        [SerializeField] private Renderer obstacleCore;
        [SerializeField] private Renderer obstacleOutline;

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

        internal override void UpdateMaterials()
        {
            obstacleCore.sharedMaterial = UIMode.PreviewMode ? distortObstacle : simpleObstacle;
            base.UpdateMaterials();
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

            obstacleCore.transform.localScale = scale - (Vector3.one * 0.01f);
            obstacleCore.transform.localPosition = cubeOffset;

            obstacleOutline.transform.localScale = scale;
            obstacleOutline.transform.localPosition = cubeOffset;

            foreach (var selectionRenderer in SelectionRenderers)
            {
                selectionRenderer.transform.localScale = scale;
                selectionRenderer.transform.localPosition = cubeOffset;
            }

            MaterialPropertyBlock.SetVector(worldScaleID, obstacleCore.transform.localScale);
            UpdateMaterials();
        }

        public float GetLength()
        {
            if (ObstacleData.CustomSize != null
                && ObstacleData.CustomSize.IsArray
                && ObstacleData.CustomSize[2].IsNumber)
                return ObstacleData.CustomSize[2];

            var length = ObstacleData.DurationSongBpm;

            //Take half jump duration into account if the setting is enabled.
            if (ObstacleData.Duration < 0 && Settings.Instance.ShowMoreAccurateFastWalls && !UIMode.AnimationMode)
                length -= length * Mathf.Abs(length / ObstacleData.Hjd);

            length *= UIMode.AnimationMode
                ? ObstacleData.EditorScale
                : EditorScaleController.EditorScale;

            return float.IsFinite(length) ? length : float.Epsilon;
        }

        public (Vector3 size, Vector3 position) ReadSizePosition()
        {
            var length = Mathf.Abs(GetLength());

            var bounds = ObstacleData.GetShape();

            return (
                new Vector3(
                    Mathf.Abs(bounds.Width),
                    Mathf.Abs(bounds.Height),
                    length
                ),
                new Vector3(
                    bounds.Position + (bounds.Width / 2.0f),
                    bounds.StartHeight + (bounds.Height < 0 ? bounds.Height : 0),
                    0
                )
            );
        }

        public override void UpdateGridPosition()
        {
            var localRotation = Vector3.zero;
            var length = GetLength();
            var (size, position) = ReadSizePosition();

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
                0,
                -0.5f,
                (ObstacleData.SongBpmTime * EditorScaleController.EditorScale) + (length < 0 ? length : 0));
            Animator.LocalTarget.localPosition = position;

            SetScale(size);

            if (localRotation != Vector3.zero)
            {
                Animator.LocalTarget.localEulerAngles = localRotation;
            }

            UpdateCollisionGroups();
        }
    }
}
