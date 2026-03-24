using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;
using Beatmap.Animations;

namespace Beatmap.Containers
{
    public abstract class ObjectContainer : MonoBehaviour
    {
        protected static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int rotationId = Shader.PropertyToID("_Rotation");

        [SerializeField] public ObjectAnimator Animator;
        [SerializeField] protected List<IntersectionCollider> Colliders;

        [Header("Visual")] [SerializeField] public VisualSettingsSO VisualSettings;
        [SerializeField] public MaterialPropertyBlockController MpbController;
        [SerializeField] public MaterialPropertyBlockController SelectionMpbController;

        private Color currentOutlineColor;
        private bool selected;

        public bool Selected
        {
            get => selected;
            set
            {
                if (selected == value) return;
                selected = value;
                HandleOutlineVisual();
            }
        }

        private bool highlighted;

        public bool Highlighted
        {
            get => highlighted;
            set
            {
                if (highlighted == value) return;
                highlighted = value;
                HandleOutlineVisual();
            }
        }

        private bool dragged;

        public bool Dragged
        {
            get => dragged;
            set
            {
                if (dragged == value) return;
                dragged = value;
                HandleOutlineVisual();
            }
        }

        public Track AssignedTrack { get; private set; }

        public abstract BaseObject ObjectData { get; set; }

        public int ChunkID => (int)(ObjectData.JsonTime / Intersections.ChunkSize);

        public void Start() => RegisterCallback();
        public void OnDestroy() => UnregisterCallback();

        protected virtual void RegisterCallback() { }
        protected virtual void UnregisterCallback() { }

        public virtual void Setup() { }

        internal virtual void SafeSetActive(bool active)
        {
            if (active != gameObject.activeSelf) gameObject.SetActive(active);
        }

        public abstract void UpdateGridPosition();

        public virtual void UpdateScalable(float scale) { }

        internal virtual void UpdateMaterials() => MpbController.ApplyChanges();

        public void SetRotation(float rot)
        {
            MpbController.Mpb.SetFloat(rotationId, rot);
            UpdateMaterials();
        }

        public void SetOutlineColor(Color color)
        {
            currentOutlineColor = color;
            HandleOutlineColor();
        }

        public virtual void AssignTrack(Track track) => AssignedTrack = track;

        protected virtual void UpdateCollisionGroups()
        {
            var chunkId = ChunkID;

            foreach (var c in Colliders)
            {
                var unregistered = Intersections.UnregisterColliderFromGroups(c);
                c.CollisionGroups.Clear();
                c.CollisionGroups.Add(chunkId);
                if (unregistered) Intersections.RegisterColliderToGroups(c);
            }
        }

        private void HandleOutlineVisual()
        {
            HandleOutlineColor();
            SelectionMpbController.ShowRenderer(selected | highlighted | dragged);
        }

        private void HandleOutlineColor()
        {
            SelectionMpbController.Mpb.SetColor(ColorId, highlighted | dragged ? Color.white : currentOutlineColor);
            SelectionMpbController.ApplyChanges();
        }
    }
}
