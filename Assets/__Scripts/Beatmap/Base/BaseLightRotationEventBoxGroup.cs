using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightRotationEventBoxGroup : BaseEventBoxGroup<BaseLightRotationEventBox>
    {
        public override ObjectType ObjectType { get; set; } = ObjectType.GLSRotation;

        public BaseLightRotationEventBoxGroup()
        {
        }

        protected BaseLightRotationEventBoxGroup(
            float time,
            int id,
            List<BaseLightRotationEventBox> boxes,
            JSONNode customData = null) : base(time, id, boxes, customData)
        {
        }

        protected BaseLightRotationEventBoxGroup(BaseLightRotationEventBoxGroup other) : base(
            other.JsonTime,
            other.ID,
            other.Boxes.Select(x => x.Clone()).Cast<BaseLightRotationEventBox>().ToList())
        {
        }

        public override string CustomKeyColor { get; } = "unusedKeyColor";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3LightRotationEventBoxGroup.ToJson(this),
            };

        public override BaseItem Clone() => new BaseLightRotationEventBoxGroup(this);
    }
}
