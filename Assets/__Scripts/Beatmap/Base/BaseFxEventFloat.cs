using System;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public class BaseFxEventFloat : BaseFxEvent<float>, IEquatable<BaseFxEventFloat>
    {
        public int Easing;

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 => V3FloatFxEvent.ToJson(this)
            };

        public override BaseItem Clone() =>
            new BaseFxEventFloat { JsonTime = JsonTime, UsePrevious = UsePrevious, Value = Value, Easing = Easing };

        public bool Equals(BaseFxEventFloat other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Easing == other.Easing
                && Mathf.Approximately(JsonTime, other.JsonTime)
                && UsePrevious == other.UsePrevious
                && Mathf.Approximately(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BaseFxEventFloat)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Easing;
                hashCode = (hashCode * 397) ^ JsonTime.GetHashCode();
                hashCode = (hashCode * 397) ^ UsePrevious;
                hashCode = (hashCode * 397) ^ Value.GetHashCode();

                return hashCode;
            }
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.Event;
        public override string CustomKeyColor => "Unused";
        public override string CustomKeyTrack => "Unused";

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false) =>
            GetHashCode() == other.GetHashCode();
    }
}
