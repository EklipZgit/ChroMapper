using System.Collections.Generic;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseVfxEventEventBoxGroup : BaseEventBoxGroup<BaseVfxEventEventBox>
    {
        public int Type { get; set; }

        public BaseVfxEventEventBoxGroup()
        {
        }

        protected BaseVfxEventEventBoxGroup(
            float time,
            int id,
            int type,
            List<BaseVfxEventEventBox> boxes,
            JSONNode customData = null) : base(time, id, boxes, customData) =>
            Type = type;

        public override string CustomKeyColor { get; } = "unusedKeyColor";
        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        public override JSONNode ToJson() => throw new System.NotImplementedException();

        public override BaseItem Clone() => throw new System.NotImplementedException();
    }
}
