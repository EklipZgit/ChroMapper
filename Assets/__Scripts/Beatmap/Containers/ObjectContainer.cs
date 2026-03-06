using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;
using Beatmap.Animations;

namespace Beatmap.Containers
{
    public abstract class ObjectContainer : MonoBehaviour
    {
        internal static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int rotationId = Shader.PropertyToID("_Rotation");
        private static readonly int outlineColorId = Shader.PropertyToID("_OutlineColor");

        public bool Dragging;

        [SerializeField] public ObjectAnimator Animator;
        [SerializeField] protected List<IntersectionCollider> Colliders;

        [Header("Visual")]
        [SerializeField] public MaterialPropertyBlockController MpbController;
        [SerializeField] public MaterialPropertyBlockController SelectionMpbController;

        public Track AssignedTrack { get; private set; }

        public abstract BaseObject ObjectData { get; set; }

        public int ChunkID => (int)(ObjectData.JsonTime / Intersections.ChunkSize);

        public abstract void UpdateGridPosition();

        public virtual void Setup() { }

        internal virtual void SafeSetActive(bool active)
        {
            if (active != gameObject.activeSelf) gameObject.SetActive(active);
        }

        public virtual void UpdateScalable(float scale) { }

        internal virtual void UpdateMaterials() => MpbController.ApplyChanges();

        public void SetRotation(float rot)
        {
            MpbController.Mpb.SetFloat(rotationId, rot);
            UpdateMaterials();
        }

        public void SetOutlineColor(Color color, bool automaticallyShowOutline = true)
        {
            if (automaticallyShowOutline) SelectionMpbController.ShowRenderer(true);
            MpbController.Mpb.SetColor(outlineColorId, color);
            MpbController.ApplyChanges();
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
    }
}
