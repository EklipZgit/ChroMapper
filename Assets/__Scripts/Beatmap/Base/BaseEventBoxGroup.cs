using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseEventBoxGroup : BaseObject
    {
        protected BaseEventBoxGroup()
        {
        }

        protected BaseEventBoxGroup(float time, int id, JSONNode customData = null) : base(
            time,
            customData) =>
            ID = id;

        public int ID;

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseEventBoxGroup eventBoxGroup && other.GetType() == GetType()) return ID == eventBoxGroup.ID;
            return false;
        }

        public abstract IReadOnlyList<BaseEventBox> ReadOnlyBoxes { get; }
    }

    public abstract class BaseEventBoxGroup<TBox> : BaseEventBoxGroup where TBox : BaseEventBox
    {
        protected BaseEventBoxGroup()
        {
        }

        protected BaseEventBoxGroup(float time, int id, JSONNode customData = null) : base(
            time,
            id,
            customData)
        {
        }

        public List<TBox> Boxes = new();

        // Cached node ordering supports deterministic outer previews and future ghost-node rendering.
        public List<BaseGLSEvent> OrderedEvents { get; private set; } = new();

        public void ResortOrderedEvents()
        {
            OrderedEvents = Boxes
                .SelectMany(box => box.ReadOnlyEvents)
                .OrderBy(evt => evt.RelativeJsonTime)
                .ThenBy(evt => evt.BoxIndex)
                .ToList();
        }

        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            // Early return if we're comparing against a different object type
            if (other is not BaseEventBoxGroup<TBox> group) return comparison;

            // Is not the same group type
            if (other.GetType() != GetType()) return comparison;

            // Compare by type if ID match
            if (comparison == 0) comparison = ID.CompareTo(group.ID);

            // TODO: I realise it is not possible and is unadvisable to sort based on event boxes,
            //  first in last out type of deal, we might have to prevent 2 GLS group in same time

            // All matching vanilla properties so compare custom data as a final check
            if (comparison == 0)
                comparison = string.Compare(
                    CustomData?.ToString(),
                    group.CustomData?.ToString(),
                    StringComparison.Ordinal);

            return comparison;
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is not BaseEventBoxGroup<TBox> group)
                return;

            ID = group.ID;
            Boxes = group.Boxes
                .Select(x => (TBox)x.Clone())
                .ToList();

            for (var i = 0; i < Boxes.Count; i++)
            {
                var box = Boxes[i];
                foreach (var evt in box.ReadOnlyEvents)
                {
                    evt.EventBoxData = box;
                    evt.EventBoxGroupData = this;
                    evt.BoxIndex = i;
                    evt.JsonTime = evt.RelativeJsonTime + JsonTime;
                }
            }

            ResortOrderedEvents();
        }

        public override IReadOnlyList<BaseEventBox> ReadOnlyBoxes => Boxes;
    }
}
