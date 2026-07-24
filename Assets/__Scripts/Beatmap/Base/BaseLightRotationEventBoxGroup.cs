using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Helper;
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
            JSONNode customData = null) : base(time, id, customData)
        {
        }

        protected BaseLightRotationEventBoxGroup(BaseLightRotationEventBoxGroup other) : base(
            other.JsonTime,
            other.ID,
            other.CustomData?.Clone())
        {
            Boxes = other.Boxes.Select(x => x.Clone()).Cast<BaseLightRotationEventBox>().ToList();
            for (var index = 0; index < Boxes.Count; index++)
            {
                var box = Boxes[index];
                foreach (var evt in box.Events)
                {
                    evt.EventBoxData = box;
                    evt.EventBoxGroupData = this;
                    evt.BoxIndex = index;
                    evt.JsonTime = evt.RelativeJsonTime + JsonTime;
                }
            }
        }

        public BaseLightRotationEventBoxGroup(JSONNode node) : this(BeatmapFactory.LightRotationEventBoxGroups(node)) { }
        
        public override void SetMap(BaseDifficulty map = null)
        {
            base.SetMap(map);
            foreach (var evt in Boxes.SelectMany(box => box.Events)) evt.SetMap(map);
        }

        public override void RecomputeSongBpmTime()
        {
            base.RecomputeSongBpmTime();
            foreach (var evt in Boxes.SelectMany(box => box.Events)) evt.RecomputeSongBpmTime();
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
