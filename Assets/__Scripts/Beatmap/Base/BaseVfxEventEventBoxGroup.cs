using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseVfxEventEventBoxGroup : BaseEventBoxGroup<BaseVfxEventEventBox>
    {
        public override ObjectType ObjectType { get; set; } = ObjectType.GLSFloatFx;
        public int Type { get; set; }

        public BaseVfxEventEventBoxGroup() => Type = 1;

        protected BaseVfxEventEventBoxGroup(
            float time,
            int id,
            int type,
            JSONNode customData = null) : base(time, id, customData) =>
            Type = type;

        protected BaseVfxEventEventBoxGroup(BaseVfxEventEventBoxGroup other) : base(
            other.JsonTime,
            other.ID,
            other.CustomData?.Clone())
        {
            Type = other.Type;
            Boxes = other.Boxes.Select(x => x.Clone()).Cast<BaseVfxEventEventBox>().ToList();
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

        // Fix: Implement ToJson to support node editor serialization for FloatFx GLS nodes
        public override JSONNode ToJson()
            => Settings.Instance.MapVersion switch
            {
                3 or 4 => V3VfxEventEventBoxGroup.ToJson(this)
            };

        public override BaseItem Clone() => new BaseVfxEventEventBoxGroup(this);
    }
}
