using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.Base
{
    public abstract class BaseGLSEvent : BaseObject
    {
        public BaseGLSEvent()
        {
        }

        protected BaseGLSEvent(float time, JSONNode customData = null) : base(time, customData) =>
            RelativeJsonTime = time;

        protected BaseGLSEvent(BaseGLSEvent other) : base(other.RelativeJsonTime, other.CustomData) =>
            RelativeJsonTime = other.RelativeJsonTime;

        public float RelativeJsonTime { get; set; }
        public override ObjectType ObjectType { get; set; } = ObjectType.GLSEvent;

        public override void RecomputeSongBpmTime()
        {
            if (EventBoxGroupData != null) jsonTime = EventBoxGroupData.JsonTime + RelativeJsonTime;
            songBpmTime = Map?.JsonTimeToSongBpmTime(JsonTime);
        }

        public override string CustomKeyColor => "unusedColor";
        public override string CustomKeyTrack => "unusedKeyTrack";

        public BaseEventBox EventBoxData;
        public BaseEventBoxGroup EventBoxGroupData;
    }
}
