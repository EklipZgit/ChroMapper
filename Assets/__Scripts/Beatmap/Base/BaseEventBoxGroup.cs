using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Rendering;

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
    }

    public abstract class BaseEventBoxGroup<T> : BaseEventBoxGroup where T : BaseEventBox
    {
        protected BaseEventBoxGroup()
        {
        }

        protected BaseEventBoxGroup(float time, int id, List<T> boxes, JSONNode customData = null) : base(
            time,
            id,
            customData) =>
            Boxes = boxes;

        public List<T> Boxes { get; set; } = new();

        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            // Early return if we're comparing against a different object type
            if (other is not BaseEventBoxGroup<T> group) return comparison;

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
    }
}
