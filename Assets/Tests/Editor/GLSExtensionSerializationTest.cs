using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using Beatmap.V4;
using NUnit.Framework;
using SimpleJSON;
using Kind = Tests.Editor.GLSExtensionEasingPlacementTest.Kind;

namespace Tests.Editor
{
    public class GLSExtensionSerializationTest
    {
        // Rotation easing must round-trip even with zero loops; the other families' extension easings do no work.
        // Check emitted JSON for both formats, with normal-node controls, without inspecting implementation text.
        [Test]
        public void ExportKeepsOnlyMeaningfulExtensionEasing(
            [Values] Kind kind,
            [Values] bool extension,
            [Values] bool v4,
            [Values(0, 2)] int loops)
        {
            var evt = CreateEvent(kind, extension, loops);
            var node = Serialize(evt, v4, out var colorCustomData);
            var keepsEasing = !extension || kind == Kind.Rotation;
            var key = v4 || kind == Kind.Rotation || kind == Kind.Translation ? "e" : "i";
            if (kind != Kind.Color || v4)
            {
                Assert.That(node.HasKey(key), Is.EqualTo(keepsEasing));
                if (keepsEasing)
                    Assert.That(node[key].AsInt, Is.EqualTo((int)EaseType.InQuadratic));
            }
            if (evt is BaseLightColorBase color)
            {
                Assert.That(colorCustomData.HasKey("easing"), Is.EqualTo(!v4 && !extension));
                foreach (var easingKey in new[] { "colorEasing", "strobeEasing", "strobeColorEasing" })
                    Assert.That(colorCustomData.HasKey(easingKey), Is.EqualTo(!extension), easingKey);
                Assert.That(colorCustomData["keep"].Value, Is.EqualTo("unrelated metadata"));
                // Export cleanup owns its JSON copy; an in-memory node and its original custom data stay editable.
                Assert.That(color.Easing, Is.EqualTo((int)EaseType.InQuadratic));
                Assert.That(color.ChromaColorEasing, Is.EqualTo((int)EaseType.InCubic));
                Assert.That(color.CustomData.HasKey("colorEasing"), Is.True);
            }
            if (evt is BaseLightRotationBase rotation)
            {
                Assert.That(node["l"].AsInt, Is.EqualTo(loops));
                Assert.That(rotation.EaseType, Is.EqualTo((int)EaseType.InQuadratic));
            }
        }

        // Direct event writers are used by node editing and V3 export; V4 common data is the real deduplicated output.
        private static JSONNode Serialize(BaseGLSEvent evt, bool v4, out JSONNode colorCustomData)
        {
            colorCustomData = new JSONObject();
            if (!v4)
            {
                var node = evt switch
                {
                    BaseLightColorBase color => V3LightColorBase.ToJson(color),
                    BaseLightRotationBase rotation => V3LightRotationBase.ToJson(rotation),
                    BaseLightTranslationBase translation => V3LightTranslationBase.ToJson(translation),
                    BaseFxEventFloat fx => V3FloatFxEvent.ToJson(fx),
                    _ => throw new System.ArgumentException(nameof(evt))
                };
                if (evt is BaseLightColorBase)
                    colorCustomData = node["customData"];
                return node;
            }
            if (evt is BaseLightColorBase v4Color)
            {
                var box = new BaseLightColorEventBox { Events = new[] { v4Color } };
                var group = new BaseLightColorEventBoxGroup { Boxes = new() { box } };
                var data = V4CommonData.LightColorEvent.FromBaseLightColorEvent(v4Color);
                var json = V4LightColorEventBoxGroup.ToJson(
                    group,
                    new List<V4CommonData.IndexFilter> { V4CommonData.IndexFilter.FromBaseIndexFilter(box.IndexFilter) },
                    new List<V4CommonData.LightColorEventBox> { V4CommonData.LightColorEventBox.FromBaseLightColorEventBox(box) },
                    new List<V4CommonData.LightColorEvent> { data });
                colorCustomData = json["e"][0]["l"][0]["customData"];
                return data.ToJson();
            }
            return evt switch
            {
                BaseLightRotationBase rotation => V4CommonData.LightRotationEvent.FromBaseLightRotationEvent(rotation).ToJson(),
                BaseLightTranslationBase translation => V4CommonData.LightTranslationEvent.FromBaseLightTranslationEvent(translation).ToJson(),
                BaseFxEventFloat fx => V4CommonData.FloatFxEvent.FromFloatFxEventBase(fx).ToJson(),
                _ => throw new System.ArgumentException(nameof(evt))
            };
        }

        // Each fixture carries a nondefault native curve and color's independent curves so stripping cannot pass by default.
        private static BaseGLSEvent CreateEvent(Kind kind, bool extension, int loops)
        {
            var previous = extension ? 1 : 0;
            return kind switch
            {
                Kind.Color => new BaseLightColorBase
                {
                    // Assign raw custom data first: its parser otherwise replaces the independent easing fixture fields.
                    CustomData = JSON.Parse("{\"keep\":\"unrelated metadata\"}"),
                    UsePrevious = previous,
                    Easing = (int)EaseType.InQuadratic,
                    ChromaColorEasing = (int)EaseType.InCubic,
                    ChromaStrobeEasing = (int)EaseType.OutBounce,
                    ChromaStrobeColorEasing = (int)EaseType.InQuadratic,
                    StrobeFade = 1
                },
                Kind.Rotation => new BaseLightRotationBase
                {
                    UsePrevious = previous, EaseType = (int)EaseType.InQuadratic, Loop = loops
                },
                Kind.Translation => new BaseLightTranslationBase
                {
                    UsePrevious = previous, EaseType = (int)EaseType.InQuadratic
                },
                _ => new BaseFxEventFloat { UsePrevious = previous, Easing = (int)EaseType.InQuadratic }
            };
        }
    }
}
