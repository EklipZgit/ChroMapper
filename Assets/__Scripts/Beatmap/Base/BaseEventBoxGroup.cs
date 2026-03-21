using System.Collections.Generic;
using Beatmap.Enums;
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

        public override ObjectType ObjectType { get; set; } = ObjectType.GLSGroup;
        public int ID { get; set; }
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

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseEventBoxGroup<T> eventBoxGroup) return ID == eventBoxGroup.ID;
            return false;
        }
    }
}
