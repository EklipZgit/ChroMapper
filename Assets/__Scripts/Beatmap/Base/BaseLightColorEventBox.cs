using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightColorEventBox : BaseEventBox
    {
        public BaseLightColorEventBox()
        {
            BrightnessDistributionType = (int)DistributionType.Wave;
            Events = Array.Empty<BaseLightColorBase>();
        }

        protected BaseLightColorEventBox(
            BaseIndexFilter indexFilter,
            float beatDistribution,
            int beatDistributionType,
            float brightnessDistribution,
            int brightnessDistributionType,
            int brightnessAffectFirst,
            BaseLightColorBase[] events) : base(indexFilter, beatDistribution, beatDistributionType)
        {
            BrightnessDistribution = brightnessDistribution;
            BrightnessDistributionType = brightnessDistributionType;
            BrightnessAffectFirst = brightnessAffectFirst;
            // Group-level load finalization removes conflicts after parent beat and lane ownership are available for diagnostics.
            Events = events;
        }

        protected BaseLightColorEventBox(
            BaseIndexFilter indexFilter,
            float beatDistribution,
            int beatDistributionType,
            float brightnessDistribution,
            int brightnessDistributionType,
            int brightnessAffectFirst,
            int easing,
            BaseLightColorBase[] events) : base(indexFilter, beatDistribution, beatDistributionType, easing)
        {
            BrightnessDistribution = brightnessDistribution;
            BrightnessDistributionType = brightnessDistributionType;
            BrightnessAffectFirst = brightnessAffectFirst;
            // Group-level load finalization removes conflicts after parent beat and lane ownership are available for diagnostics.
            Events = events;
        }

        protected BaseLightColorEventBox(BaseLightColorEventBox other) : base(
            other.IndexFilter.Clone() as BaseIndexFilter,
            other.BeatDistribution,
            other.BeatDistributionType,
            other.Easing)
        {
            BrightnessDistribution = other.BrightnessDistribution;
            BrightnessDistributionType = other.BrightnessDistributionType;
            BrightnessAffectFirst = other.BrightnessAffectFirst;
            // V3ColorBoxRoundTripPreservesShiftPayloadAndUnknownCustomData requires duplicate boxes to own independent custom payloads and parsed caches.
            CustomData = other.CustomData?.Clone();
            Events = other.Events.Select(x => x.Clone()).Cast<BaseLightColorBase>().ToArray();
        }

        public float BrightnessDistribution { get; set; }
        public int BrightnessDistributionType { get; set; }
        public int BrightnessAffectFirst { get; set; }
        public BaseLightColorBase[] Events { get; set; }
        // V3ColorBoxRoundTripPreservesShiftPayloadAndUnknownCustomData keeps raw box data authoritative while parsed lists serve playback.
        public JSONNode CustomData { get; private set; } = new JSONObject();
        public string[] Shifts { get; set; } = Array.Empty<string>();
        public string[] StrobeShifts { get; set; } = Array.Empty<string>();
        public IReadOnlyList<GLSColorShiftInstruction> ParsedShifts { get; private set; } =
            Array.Empty<GLSColorShiftInstruction>();
        public IReadOnlyList<GLSColorShiftInstruction> ParsedStrobeShifts { get; private set; } =
            Array.Empty<GLSColorShiftInstruction>();

        // Box-level JSON owns extensions that apply to every child event while retaining unrelated forward-compatible data.
        public void SetCustomData(JSONNode customData)
        {
            CustomData = customData is JSONObject
                ? customData
                : new JSONObject();
            Shifts = GLSColorShift.ReadStrings(CustomData, GLSColorShift.ShiftsKey);
            StrobeShifts = GLSColorShift.ReadStrings(CustomData, GLSColorShift.StrobeShiftsKey);
            RefreshShiftCaches();
        }

        // Editor changes rewrite only the two owned arrays and refresh playback caches before the group replacement action runs.
        public JSONNode SaveCustom()
        {
            GLSColorShift.WriteStrings(CustomData, GLSColorShift.ShiftsKey, Shifts);
            GLSColorShift.WriteStrings(CustomData, GLSColorShift.StrobeShiftsKey, StrobeShifts);
            RefreshShiftCaches();
            return CustomData;
        }

        private void RefreshShiftCaches()
        {
            ParsedShifts = GLSColorShift.Parse(Shifts);
            ParsedStrobeShifts = GLSColorShift.Parse(StrobeShifts);
        }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3LightColorEventBox.ToJson(this),
            };

        public override BaseItem Clone() => new BaseLightColorEventBox(this);

        public override IReadOnlyList<BaseGLSEvent> ReadOnlyEvents => Events;

        public override void ClearEvents() => Events = Array.Empty<BaseLightColorBase>();

        // Color-lane mutations use the shared occupied-beat replacement invariant before restoring their typed array.
        public override void SetEvents(BaseGLSEvent[] data) =>
            Events = ResolveSameBeatConflicts(data).OfType<BaseLightColorBase>().ToArray();
    }
}
