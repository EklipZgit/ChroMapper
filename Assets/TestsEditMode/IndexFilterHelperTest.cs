// Empty and malformed GLS filters must fail before iteration so regression tests cannot repeat the billion-iteration stall.
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace TestsEditMode
{
    public class IndexFilterHelperTest
    {
        // The rotation and translation axis fixtures select missing environment groups with chunked, reversed Division filters.
        [TestCase(1, 4, 2, 1, 3)]
        [TestCase(1, 5, 3, 1, 2)]
        [TestCase(1, 4, 2, 0, 3)]
        [TestCase(1, 1, 0, 0, 0)]
        [TestCase(1, 1, 0, 1, 0)]
        [TestCase(2, 0, 1, 0, 3)]
        [TestCase(2, 0, 1, 1, 3)]
        [TestCase(2, 0, 0, 0, 0)]
        public void EmptyGroupReturnsNoFilter(int type, int param0, int param1, int reverse, int chunks)
        {
            var filter = IndexFilterHelper.Convert(new BaseIndexFilter(type, param0, param1, reverse, chunks), 0);

            // NUnit formats IEnumerable values on failure; assert a boolean so reporting cannot enumerate the broken filter.
            Assert.That(filter == null, Is.True, "A missing environment group must be rejected before chunk division or enumeration.");
        }

        // Invalid parameters share the empty-group failure mode: they must never become huge or backwards ranges.
        [TestCase(0, 0, 0, 0, 10)]
        [TestCase(-1, 0, 0, 0, 10)]
        [TestCase(2, -1, 0, 0, 10)]
        [TestCase(2, 2, 0, 0, 10)]
        [TestCase(2, int.MaxValue, 1, 0, 10)]
        [TestCase(1, 0, 0, -1, 10)]
        [TestCase(1, 0, 0, 0, -1)]
        public void InvalidDivisionParametersReturnNoFilter(int sections, int sectionId, int reverse, int chunks, int groupSize)
        {
            var filter = IndexFilterHelper.Convert(
                new BaseIndexFilter((int)IndexFilterType.Division, sections, sectionId, reverse, chunks), groupSize);

            // Failure output must not enumerate an invalid filter while explaining that it should have been rejected.
            Assert.That(filter == null, Is.True, "Invalid Division parameters must not create an iterable range.");
        }

        // A legal section number can still lie beyond the available chunks; returning a reversed range invents lights.
        [TestCase(0)]
        [TestCase(1)]
        public void EmptyTrailingDivisionSectionReturnsNoFilter(int reverse)
        {
            var filter = IndexFilterHelper.Convert(
                new BaseIndexFilter((int)IndexFilterType.Division, 5, 3, reverse, 2), 10);

            // Failure output must not enumerate the invalid range it is reporting.
            Assert.That(filter == null, Is.True, "A section beyond the available chunks must not select neighbouring or negative IDs.");
        }

        // Negative offsets can overflow the range size, and negative steps cannot represent forward offset selection.
        [TestCase(-1, 1)]
        [TestCase(int.MinValue, 1)]
        [TestCase(0, -1)]
        [TestCase(10, 1)]
        public void InvalidStepAndOffsetParametersReturnNoFilter(int offset, int step)
        {
            var filter = IndexFilterHelper.Convert(
                new BaseIndexFilter((int)IndexFilterType.StepAndOffset, offset, step, 0), 10);

            // Failure output must not enumerate an invalid or negative-sized range.
            Assert.That(filter == null, Is.True, "Invalid StepAndOffset parameters must not create an iterable range.");
        }

        // Guarding invalid input must preserve normal chunk expansion, reverse order, and a partially filled final chunk.
        [TestCase(1, 3, 2, 0, 0, new[] { 8, 9 }, 2)]
        [TestCase(1, 3, 2, 1, 0, new[] { 1, 0 }, 2)]
        [TestCase(1, 2, 0, 0, 3, new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, 2)]
        [TestCase(1, 2, 0, 1, 3, new[] { 8, 9, 4, 5, 6, 7 }, 2)]
        [TestCase(2, 1, 1, 0, 3, new[] { 4, 5, 6, 7, 8, 9 }, 2)]
        [TestCase(2, 1, 1, 1, 3, new[] { 4, 5, 6, 7, 0, 1, 2, 3 }, 2)]
        [TestCase(2, 2, 0, 0, 0, new[] { 2 }, 1)]
        [TestCase(2, 2, 0, 1, 0, new[] { 7 }, 1)]
        public void ValidFiltersPreserveSelectedElements(
            int type, int param0, int param1, int reverse, int chunks, int[] expectedElements, int expectedCount)
        {
            var filter = IndexFilterHelper.Convert(new BaseIndexFilter(type, param0, param1, reverse, chunks), 10);

            Assert.That(filter, Is.Not.Null);
            Assert.That(filter.Count, Is.EqualTo(expectedCount), "Check the bound before enumerating so a regression fails promptly.");
            Assert.That(filter.VisibleCount, Is.EqualTo(expectedCount));
            Assert.That(filter.Select(item => item.Element).ToArray(), Is.EqualTo(expectedElements));
        }

        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases locks the requested 0,0,1,1 chunk and 0,1,2,3 light orders for selected physical lights 0,1,4,5.
        [Test]
        public void ModeBColorShiftsUseDenseAffectedChunkOrder()
        {
            var filter = IndexFilterHelper.Convert(
                new BaseIndexFilter((int)IndexFilterType.StepAndOffset, 0, 2, 0, 4),
                8);
            var entries = filter.ToArray();

            Assert.That(entries.Select(entry => entry.Element).ToArray(), Is.EqualTo(new[] { 0, 1, 4, 5 }));
            Assert.That(entries.Select(entry => entry.AffectedChunkOrder).ToArray(), Is.EqualTo(new[] { 0, 0, 1, 1 }));
            Assert.That(entries.Select(entry => entry.AffectedLightOrder).ToArray(), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.AreEqual(2, filter.VisibleCount);
            Assert.AreEqual(4, filter.AffectedLightCount);
        }

        // OptionalLightProgressFieldAcceptsForwardCompatibleTokensAndTrailingFields protects the optional fourth slot without relaxing required offset/easing validation.
        [Test]
        public void OptionalLightProgressFieldAcceptsForwardCompatibleTokensAndTrailingFields()
        {
            var instructions = GLSColorShift.Parse(new[]
            {
                "xg?,0.2,lin,l,ignored",
                "r,0.1,lin,future,ignored",
                "b,0.3,lin,l,ignored,again",
                "h,NaN,lin,l",
                "s,0.4,futureEase,l",
                "?,0.5,lin,l"
            });

            Assert.AreEqual(3, instructions.Count);
            Assert.That(instructions[0].Targets, Is.EqualTo(GLSColorShiftTargets.Green));
            Assert.That(instructions[0].Offset, Is.EqualTo(0.2f));
            Assert.That(instructions[1].Targets, Is.EqualTo(GLSColorShiftTargets.Red));
            Assert.That(instructions[2].Targets, Is.EqualTo(GLSColorShiftTargets.Blue));
        }

        // OptionalProgressModesRemainTolerantWithoutChangingRequiredFields checks the fourth slot alone controls the mode, and malformed required fields never become active instructions.
        [TestCase("xg?,0.2,lin", true, false)]
        [TestCase("xg?,0.2,lin,l", true, true)]
        [TestCase("xg?,0.2,lin, L ,discard", true, true)]
        [TestCase("xg?,0.2,lin,future,l", true, false)]
        [TestCase("xg?,0.2,lin,,l", true, false)]
        [TestCase("xg?,0.2,lin,ll", true, false)]
        [TestCase("xg?,NaN,lin,l", false, false)]
        [TestCase("xg?,Infinity,lin,l", false, false)]
        [TestCase("xg?,oops,lin,l", false, false)]
        [TestCase("xg?,,lin,l", false, false)]
        [TestCase("xg?,0.2,,l", false, false)]
        [TestCase("xg?,0.2,futureEase,l", false, false)]
        [TestCase("xg?,0.2", false, false)]
        [TestCase("x?,0.2,lin,l", false, false)]
        [TestCase(null, false, false)]
        public void OptionalProgressModesRemainTolerantWithoutChangingRequiredFields(
            string text, bool valid, bool perLight)
        {
            var instructions = GLSColorShift.Parse(new[] { text });
            Assert.That(instructions.Count, Is.EqualTo(valid ? 1 : 0));
            if (!valid)
            {
                return;
            }

            Assert.That(instructions[0].UsesAffectedLightProgress, Is.EqualTo(perLight));
            var color = GLSColorShift.Apply(Color.black, instructions, null, 0f, 0.5f);
            Assert.That(color.g, Is.EqualTo(perLight ? 0.1f : 0f).Within(0.0001f));
        }

        // AffectedLightCountMatchesPartialReversedAndLimitedChunks protects the per-light denominator before enumeration, including seed-based selection of a short final chunk.
        [TestCase(0, 0, 0f)]
        [TestCase(1, 0, 0f)]
        [TestCase(0, 0, 0.5f)]
        [TestCase(1, 0, 0.5f)]
        [TestCase(0, 1, 0.5f)]
        [TestCase(1, 1, 0.5f)]
        [TestCase(0, 2, 0.5f)]
        [TestCase(1, 2, 0.5f)]
        [TestCase(0, 3, 0.5f)]
        [TestCase(1, 3, 0.5f)]
        public void AffectedLightCountMatchesPartialReversedAndLimitedChunks(int reverse, int random, float limit)
        {
            var filter = IndexFilterHelper.Convert(
                new BaseIndexFilter((int)IndexFilterType.StepAndOffset, 0, 1, reverse, 4, limit, 0, random, 12),
                10);
            var countBeforeEnumeration = filter.AffectedLightCount;
            var entries = filter.ToArray();

            Assert.That(countBeforeEnumeration, Is.EqualTo(entries.Length));
            Assert.That(filter.AffectedLightCount, Is.EqualTo(countBeforeEnumeration));
            Assert.That(entries.Select(entry => entry.AffectedLightOrder), Is.EqualTo(Enumerable.Range(0, entries.Length)));
            if (random == 0 && limit == 0f)
            {
                Assert.That(entries.Length, Is.EqualTo(10));
            }
            if (random == 0 && limit == 0.5f)
            {
                Assert.That(entries.Length, Is.EqualTo(reverse == 0 ? 6 : 4));
            }
        }

        // PerLightShiftsPreserveHsvHdrAndIndependentF applies different chunk/light coordinates so hue wrapping, saturation clamping, HDR value, f, and box-before-event order cannot regress unnoticed.
        [Test]
        public void PerLightShiftsPreserveHsvHdrAndIndependentF()
        {
            var source = Color.HSVToRGB(0.1f, 0.8f, 2f, true);
            source.a = 0.25f;
            var color = GLSColorShift.Apply(
                source,
                GLSColorShift.Parse(new[] { "xh,-0.6,lin,l", "s,0.6,lin,l", "f,0.6,lin,l" }),
                GLSColorShift.Parse(new[] { "s,-0.4,lin,future", "v,2,lin,l", "f,0.8,iq,l" }),
                1f,
                0.5f);
            Color.RGBToHSV(color, out var hue, out var saturation, out var value);

            Assert.That(hue, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(saturation, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(value, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(color.a, Is.EqualTo(0.75f).Within(0.0001f));
        }

        // FirstColorModelWinsWhileFRemainsIndependent covers rs/sr compatibility, forward-compatible unknown targets, and f composition with a color model.
        [Test]
        public void FirstColorModelWinsWhileFRemainsIndependent()
        {
            var rgbFirst = GLSColorShift.Apply(
                new Color(0.2f, 0.3f, 0.4f, 0.5f),
                GLSColorShift.Parse(new[] { "xrsf,0.3,lin" }),
                null,
                1f);
            var hsvSource = Color.HSVToRGB(0.1f, 0.2f, 0.5f, true);
            var hsvFirst = GLSColorShift.Apply(
                hsvSource,
                GLSColorShift.Parse(new[] { "xsfr,0.3,lin" }),
                null,
                1f);
            Color.RGBToHSV(hsvFirst, out var hue, out var saturation, out var value);

            Assert.That(rgbFirst.r, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(rgbFirst.g, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(rgbFirst.b, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(rgbFirst.a, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(hue, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(saturation, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(value, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(hsvFirst.a, Is.EqualTo(1.3f).Within(0.0001f));
        }

        // NormalAndStrobeShiftsRemainIndependent locks box-before-event composition and the non-multicolor strobe fallback to the unshifted main color.
        [Test]
        public void NormalAndStrobeShiftsRemainIndependent()
        {
            var box = new BaseLightColorEventBox();
            box.SetCustomData(JSON.Parse(
                "{\"shifts\":[\"g,0.1,lin\"],\"strobeShifts\":[\"f,0.5,lin\"]}"));
            var evt = V3LightColorBase.GetFromJson(JSON.Parse(
                "{\"customData\":{\"shifts\":[\"r,0.2,lin\"],\"strobeShifts\":[\"b,0.3,lin\"]}}"));
            var mainColor = new Color(0.1f, 0.2f, 0.3f, 0.4f);

            var normal = GLSColorShift.ApplyNormal(mainColor, box, evt, 1f);
            var strobe = GLSColorShift.ApplyStrobe(mainColor, box, evt, 1f);

            Assert.That(normal.r, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(normal.g, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(normal.b, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(normal.a, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(strobe.r, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(strobe.g, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(strobe.b, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(strobe.a, Is.EqualTo(0.9f).Within(0.0001f));
        }

        // OrderedShiftInstructionsAccumulateWithIndependentEasings proves list order and each instruction's spatial easing are evaluated independently at one chunk coordinate.
        [Test]
        public void OrderedShiftInstructionsAccumulateWithIndependentEasings()
        {
            var color = GLSColorShift.Apply(
                new Color(0.25f, 0.5f, 0.75f, 1f),
                GLSColorShift.Parse(new[] { "r,0.2,lin", "r,0.4,iq" }),
                null,
                0.5f);

            Assert.That(color.r, Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(color.g, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(color.b, Is.EqualTo(0.75f).Within(0.0001f));
        }

        // InvalidShiftInstructionsAreIgnoredWithoutRejectingRecognizedTargets ensures malformed fields are inert while unknown target characters remain forward-compatible.
        [Test]
        public void InvalidShiftInstructionsAreIgnoredWithoutRejectingRecognizedTargets()
        {
            var instructions = GLSColorShift.Parse(new[]
            {
                "xg?,0.2,lin",
                "r,NaN,lin",
                "b,0.1,futureEase",
                "?,0.5,lin"
            });
            var color = GLSColorShift.Apply(new Color(0.1f, 0.2f, 0.3f, 1f), instructions, null, 1f);

            Assert.AreEqual(1, instructions.Count);
            Assert.That(color.r, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(color.g, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(color.b, Is.EqualTo(0.3f).Within(0.0001f));
        }
    }
}
