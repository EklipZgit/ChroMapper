using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseVfxEventEventBoxGroup : BaseEventBoxGroup<BaseVfxEventEventBox>
    {
        public override ObjectType ObjectType { get; set; } = ObjectType.GLSFloatFx;
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

        protected BaseVfxEventEventBoxGroup(BaseVfxEventEventBoxGroup other) : base(
            other.JsonTime,
            other.ID,
            other.Boxes.Select(x => x.Clone()).Cast<BaseVfxEventEventBox>().ToList())
        {
        }

        public override string CustomKeyColor { get; } = "unusedKeyColor";
        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        public override JSONNode ToJson() => throw new System.NotImplementedException();

        public override BaseItem Clone() => new BaseVfxEventEventBoxGroup(this);
    }
}
