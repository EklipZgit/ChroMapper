using System;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightTranslationBase : BaseGLSEvent
    {
        public BaseLightTranslationBase()
        {
        }

        // Used for Node Editor
        public BaseLightTranslationBase(JSONNode node) : this(V3LightTranslationBase.GetFromJson(node))
        {
        }

        protected BaseLightTranslationBase(
            float time,
            float translation,
            int easeType,
            int usePrevious,
            JSONNode customData = null) : base(time, customData)
        {
            Translation = translation;
            EaseType = easeType;
            UsePrevious = usePrevious;
        }

        protected BaseLightTranslationBase(BaseLightTranslationBase other) : base(other)
        {
            Translation = other.Translation;
            EaseType = other.EaseType;
            UsePrevious = other.UsePrevious;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.GLSEvent;
        public float Translation { get; set; }
        public int EaseType { get; set; }
        public int UsePrevious { get; set; }

        public override string CustomKeyColor { get; } = "unusedColor";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is not BaseLightTranslationBase other)
                return;

            Translation = other.Translation;
            EaseType = other.EaseType;
            UsePrevious = other.UsePrevious;
        }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseLightTranslationBase ltb) return BoxIndex == ltb.BoxIndex;
            return false;
        }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3LightTranslationBase.ToJson(this),
            };

        public override BaseItem Clone() => new BaseLightTranslationBase(this);
    }
}
