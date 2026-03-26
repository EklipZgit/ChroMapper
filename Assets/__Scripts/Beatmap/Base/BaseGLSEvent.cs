using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseGLSEvent : BaseObject
    {
        public BaseGLSEvent()
        {
        }

        protected BaseGLSEvent(float relativeTime, float time, JSONNode customData = null) : base(time, customData) =>
            RelativeJsonTime = relativeTime;

        public float RelativeJsonTime { get; set; }
        public override ObjectType ObjectType { get; set; } = ObjectType.GLSEvent;

        public override void RecomputeSongBpmTime()
        {
            if (EventBoxGroupData != null) jsonTime = EventBoxGroupData.JsonTime + RelativeJsonTime;
            base.RecomputeSongBpmTime();
        }

        public override string CustomKeyColor => "unusedColor";
        public override string CustomKeyTrack => "unusedKeyTrack";

        public BaseEventBox EventBoxData;
        public BaseEventBoxGroup EventBoxGroupData;
        public int BoxIndex = -1;
    }
}
