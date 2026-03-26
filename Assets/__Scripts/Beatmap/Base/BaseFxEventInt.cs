using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseFxEventInt : BaseFxEvent<int>
    {
        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 => V3IntFxEvent.ToJson(this)
            };

        public override BaseItem Clone() =>
            new BaseFxEventInt { RelativeJsonTime = RelativeJsonTime, UsePrevious = UsePrevious, Value = Value };

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;
        public override string CustomKeyColor => "Unused";
        public override string CustomKeyTrack => "Unused";

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false) =>
            GetHashCode() == other.GetHashCode();
    }
}
