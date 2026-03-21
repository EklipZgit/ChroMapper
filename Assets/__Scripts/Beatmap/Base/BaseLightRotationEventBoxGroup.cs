using System.Collections.Generic;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightRotationEventBoxGroup : BaseEventBoxGroup<BaseLightRotationEventBox>
    {
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

        public override string CustomKeyColor { get; } = "unusedKeyColor";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 => V3LightRotationEventBoxGroup.ToJson(this),
            };

        public override BaseItem Clone() => throw new System.NotImplementedException();
    }
}
