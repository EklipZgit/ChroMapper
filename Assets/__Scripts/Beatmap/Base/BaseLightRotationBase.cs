using System;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightRotationBase : BaseGLSEvent
    {
        public BaseLightRotationBase()
        {
        }

        protected BaseLightRotationBase(
            float time,
            float rotation,
            int direction,
            int easeType,
            int loop,
            int usePrevious,
            JSONNode customData = null) : base(time, customData)
        {
            Rotation = rotation;
            Direction = direction;
            EaseType = easeType;
            Loop = loop;
            UsePrevious = usePrevious;
        }

        protected BaseLightRotationBase(BaseLightRotationBase other) : base(other.RelativeJsonTime, other.JsonTime, other.CustomData)
        {
            Rotation = other.Rotation;
            Direction = other.Direction;
            EaseType = other.EaseType;
            Loop = other.Loop;
            UsePrevious = other.UsePrevious;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.GLSEvent;
        public float Rotation { get; set; }
        public int Direction { get; set; }
        public int EaseType { get; set; }
        public int Loop { get; set; }
        public int UsePrevious { get; set; }

        public override string CustomKeyColor { get; } = "unusedColor";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";


        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseLightRotationBase lrb)
            {
                return Math.Abs(Rotation - lrb.Rotation) < DecimalTolerance
                    || Direction == lrb.Direction
                    || EaseType == lrb.EaseType
                    || Loop == lrb.Loop
                    || UsePrevious == lrb.UsePrevious;
            }

            return false;
        }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3LightRotationBase.ToJson(this)
            };

        public override BaseItem Clone() => new BaseLightRotationBase(this);
    }
}
