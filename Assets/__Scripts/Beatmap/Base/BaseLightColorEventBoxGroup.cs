using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightColorEventBoxGroup : BaseEventBoxGroup<BaseLightColorEventBox>
    {
        public override ObjectType ObjectType { get; set; } = ObjectType.GLSColor;

        public BaseLightColorEventBoxGroup()
        {
        }

        protected BaseLightColorEventBoxGroup(
            float time,
            int id,
            List<BaseLightColorEventBox> boxes,
            JSONNode customData = null) : base(time, id, boxes, customData)
        {
        }

        protected BaseLightColorEventBoxGroup(BaseLightColorEventBoxGroup other) : base(
            other.JsonTime,
            other.ID,
            other.Boxes.Select(x => x.Clone()).Cast<BaseLightColorEventBox>().ToList())
        {
        }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 => V3LightColorEventBoxGroup.ToJson(this),
            };

        public override BaseItem Clone() => new BaseLightColorEventBoxGroup(this);

        public override string CustomKeyColor { get; } = "unusedKeyColor";
        public override string CustomKeyTrack { get; } = "unusedKeyTrack";
    }
}
