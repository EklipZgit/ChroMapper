using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    // FloatFX data is mutable and keys active-container dictionaries, so it must retain reference equality across lanes.
    public class BaseFxEventFloat : BaseFxEvent<float>
    {
        public BaseFxEventFloat()
        {
        }

        // Used for Node Editor
        public BaseFxEventFloat(JSONNode node) : this(V3FloatFxEvent.GetFromJson(node))
        {
        }

        protected BaseFxEventFloat(
            float time,
            float value,
            int easing,
            int usePrevious,
            JSONNode customData = null) : base(time, value, customData)
        {
            Value = value;
            Easing = easing;
            UsePrevious = usePrevious;
        }

        protected BaseFxEventFloat(BaseFxEventFloat other) : base(other) => Easing = other.Easing;

        public int Easing;

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is not BaseFxEventFloat other)
                return;

            Easing = other.Easing;
        }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3FloatFxEvent.ToJson(this)
            };

        public override BaseItem Clone() => new BaseFxEventFloat(this);

        // FloatFX nodes live in the shared inner GLS-event collection; Event routes quick-delete to the wrong grid.
        public override ObjectType ObjectType { get; set; } = ObjectType.GLSEvent;
        public override string CustomKeyColor => "Unused";
        public override string CustomKeyTrack => "Unused";

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseFxEventFloat fx) return BoxIndex == fx.BoxIndex;
            return false;
        }
    }
}
