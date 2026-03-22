using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseVfxEventEventBox : BaseEventBox
    {
        public BaseVfxEventEventBox()
        {
            IndexFilter = new BaseIndexFilter();
            Events = Array.Empty<BaseFxEventFloat>();
        }

        protected BaseVfxEventEventBox(
            BaseIndexFilter indexFilter,
            float beatDistribution,
            int beatDistributionType,
            float vfxDistribution,
            int vfxDistributionType,
            int vfxAffectFirst,
            IList<BaseFxEventFloat> floatFxEvents) : base(
            indexFilter,
            beatDistribution,
            beatDistributionType)
        {
            VfxDistribution = vfxDistribution;
            VfxDistributionType = vfxDistributionType;
            VfxAffectFirst = vfxAffectFirst;
            Events = Events.Select(e => (BaseFxEventFloat)e.Clone()).ToArray();
        }

        protected BaseVfxEventEventBox(
            BaseIndexFilter indexFilter,
            float beatDistribution,
            int beatDistributionType,
            float vfxDistribution,
            int vfxDistributionType,
            int vfxAffectFirst,
            int easing,
            IList<BaseFxEventFloat> floatFxEvents) : base(
            indexFilter,
            beatDistribution,
            beatDistributionType,
            easing)
        {
            VfxDistribution = vfxDistribution;
            VfxDistributionType = vfxDistributionType;
            VfxAffectFirst = vfxAffectFirst;
            Events = Events.Select(e => (BaseFxEventFloat)e.Clone()).ToArray();
        }

        protected BaseVfxEventEventBox(BaseVfxEventEventBox other) : base(
            other.IndexFilter.Clone() as BaseIndexFilter,
            other.BeatDistribution,
            other.BeatDistributionType,
            other.Easing)
        {
            VfxDistribution = other.VfxDistribution;
            VfxDistributionType = other.VfxDistributionType;
            VfxAffectFirst = other.VfxAffectFirst;
            Events = other.Events.Select(x => x.Clone()).Cast<BaseFxEventFloat>().ToArray();
        }

        public float VfxDistribution { get; set; }
        public int VfxDistributionType { get; set; }
        public int VfxAffectFirst { get; set; }

        public BaseFxEventFloat[] Events { get; set; } = Array.Empty<BaseFxEventFloat>();


        public override JSONNode ToJson() => throw new System.NotImplementedException();

        public override BaseItem Clone() => new BaseVfxEventEventBox(this);
    }
}
