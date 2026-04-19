using System.Collections.Generic;
using Beatmap.Enums;

namespace Beatmap.Base
{
    public abstract class BaseEventBox : BaseItem
    {
        protected BaseEventBox()
        {
            IndexFilter = new BaseIndexFilter();
            BeatDistributionType = (int)DistributionType.Wave;
        }

        protected BaseEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType)
        {
            IndexFilter = indexFilter;
            BeatDistribution = beatDistribution;
            BeatDistributionType = beatDistributionType;
            Easing = 0;
        }

        protected BaseEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            int easing)
        {
            IndexFilter = indexFilter;
            BeatDistribution = beatDistribution;
            BeatDistributionType = beatDistributionType;
            Easing = easing;
        }

        public BaseIndexFilter IndexFilter { get; set; }
        public float BeatDistribution { get; set; }
        public int BeatDistributionType { get; set; }

        public int Easing { get; set; }
        
        public abstract IReadOnlyList<BaseGLSEvent> ReadOnlyEvents { get; }
        public abstract void ClearEvents();
        public abstract void SetEvents(BaseGLSEvent[] data);

        public virtual Axis GetAxis() => Axis.X;

    }
}
