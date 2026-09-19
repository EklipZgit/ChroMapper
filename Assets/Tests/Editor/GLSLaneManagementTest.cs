using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using NUnit.Framework;
using Tests.Infrastructure;
using TMPro;
using UnityEngine;

namespace Tests.Editor
{
    public class GLSLaneManagementTest : TestBase
    {
        private const string AddLaneTooltip =
            "Add a lane following the currently selected event box lane below. Lights are owned by the first lane whose filter matches them.";

        // AddLaneAfterSelectedTransformLanePreservesAxis prevents the new lane from jumping into the X section
        // when the selected authored lane is Y or Z.
        [TestCase(false, 1)]
        [TestCase(false, 2)]
        [TestCase(true, 1)]
        [TestCase(true, 2)]
        public void AddLaneAfterSelectedTransformLanePreservesAxis(bool translation, int selectedAxis)
        {
            BaseEventBoxGroup group = translation
                ? new BaseLightTranslationEventBoxGroup
                {
                    Boxes =
                    {
                        new BaseLightTranslationEventBox { Axis = 0, BeatDistribution = 10 },
                        new BaseLightTranslationEventBox { Axis = selectedAxis, BeatDistribution = 20 },
                        new BaseLightTranslationEventBox { Axis = selectedAxis, BeatDistribution = 30 }
                    }
                }
                : new BaseLightRotationEventBoxGroup
                {
                    Boxes =
                    {
                        new BaseLightRotationEventBox { Axis = 0, BeatDistribution = 10 },
                        new BaseLightRotationEventBox { Axis = selectedAxis, BeatDistribution = 20 },
                        new BaseLightRotationEventBox { Axis = selectedAxis, BeatDistribution = 30 }
                    }
                };
            group.JsonTime = 3000 + selectedAxis + (translation ? 10 : 0);
            group.songBpmTime = group.JsonTime;
            if (group is BaseLightTranslationEventBoxGroup translationGroup)
            {
                translationGroup.NormalizeLoadedEventConflicts();
            }
            else if (group is BaseLightRotationEventBoxGroup rotationGroup)
            {
                rotationGroup.NormalizeLoadedEventConflicts();
            }
            // AddLaneAfterSelectedTransformLanePreservesAxis invokes the undoable production command on a real collection member so ghost-object diagnostics cannot mask the axis assertion.
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType)
                .SpawnObject(group, false, false, true);
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = group;

            var editedGroup = GLSEventBoxCommand.AddEventBox(group, 2);

            Assert.AreEqual(4, editedGroup.ReadOnlyBoxes.Count);
            Assert.AreEqual(10, editedGroup.ReadOnlyBoxes[0].BeatDistribution);
            Assert.AreEqual(20, editedGroup.ReadOnlyBoxes[1].BeatDistribution);
            Assert.AreEqual(selectedAxis, (int)editedGroup.ReadOnlyBoxes[2].GetAxis());
            Assert.AreEqual(0, editedGroup.ReadOnlyBoxes[2].ReadOnlyEvents.Count);
            Assert.AreEqual(30, editedGroup.ReadOnlyBoxes[3].BeatDistribution);

            Object.FindAnyObjectByType<BeatmapActionContainer>().Undo();

            Assert.AreEqual(3, provider.GroupContext.ReadOnlyBoxes.Count);
            Assert.AreEqual(selectedAxis, (int)provider.GroupContext.ReadOnlyBoxes[1].GetAxis());
            Assert.AreEqual(30, provider.GroupContext.ReadOnlyBoxes[2].BeatDistribution);
        }

        // LaterOverlappingLaneLabelIsBrightRed covers the shared first-filter-wins warning for every GLS lane type.
        [TestCase("Color")]
        [TestCase("Rotation")]
        [TestCase("Translation")]
        [TestCase("FloatFX")]
        public void LaterOverlappingLaneLabelIsBrightRed(string laneType)
        {
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var effectHost = RegisterFourLightEffect(context, laneType, out var groupId);
            try
            {
                provider.GroupContext = CreateOverlappingGroup(laneType, groupId);

                var labels = GetDisplayedLabels(provider);

                Assert.GreaterOrEqual(labels.Length, laneType is "Rotation" or "Translation" ? 3 : 2);
                Assert.AreNotEqual(Color.red, labels[0].color);
                if (laneType is "Rotation" or "Translation")
                {
                    Assert.AreNotEqual(
                        Color.red,
                        labels[1].color,
                        "An earlier X lane must not consume the same light IDs on Y.");
                    Assert.AreEqual(Color.red, labels[2].color);
                }
                else
                {
                    Assert.AreEqual(Color.red, labels[1].color);
                }
            }
            finally
            {
                UnregisterEffect(context, laneType, groupId);
                Object.DestroyImmediate(effectHost);
            }
        }

        // AddLaneButtonExplainsOrderingAndOwnership keeps the insertion position and first-match rule discoverable.
        [Test]
        public void AddLaneButtonExplainsOrderingAndOwnership()
        {
            var controller = Object.FindAnyObjectByType<EventBoxViewController>();
            var buttonField = typeof(EventBoxViewController).GetField(
                "addEventBoxButton",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var button = buttonField.GetValue(controller) as ButtonComponent;
            var tooltips = button.GetComponents<Tooltip>();

            Assert.That(
                tooltips.Any(tooltip => tooltip.TooltipOverride == AddLaneTooltip),
                Is.True,
                "The add-lane button needs the requested ownership explanation.");
            Assert.That(
                tooltips.Any(tooltip => tooltip.AdvancedTooltip == AddLaneTooltip),
                Is.True,
                "The advanced tooltip must preserve the same explanation.");
        }

        // CreateOverlappingGroup authors two valid all-light lanes on the same subtype so only the first can own lights.
        private static BaseEventBoxGroup CreateOverlappingGroup(string laneType, int groupId) => laneType switch
        {
            "Color" => new BaseLightColorEventBoxGroup
            {
                ID = groupId,
                Boxes = { new BaseLightColorEventBox(), new BaseLightColorEventBox() }
            },
            "Rotation" => new BaseLightRotationEventBoxGroup
            {
                ID = groupId,
                Boxes =
                {
                    new BaseLightRotationEventBox { Axis = 0 },
                    new BaseLightRotationEventBox { Axis = 1 },
                    new BaseLightRotationEventBox { Axis = 1 }
                }
            },
            "Translation" => new BaseLightTranslationEventBoxGroup
            {
                ID = groupId,
                Boxes =
                {
                    new BaseLightTranslationEventBox { Axis = 0 },
                    new BaseLightTranslationEventBox { Axis = 1 },
                    new BaseLightTranslationEventBox { Axis = 1 }
                }
            },
            "FloatFX" => new BaseVfxEventEventBoxGroup
            {
                ID = groupId,
                Boxes = { new BaseVfxEventEventBox(), new BaseVfxEventEventBox() }
            },
            _ => throw new AssertionException($"Unknown GLS lane type {laneType}.")
        };

        // RegisterFourLightEffect gives the label renderer a deterministic physical group without relying on the test map's environment.
        private static GameObject RegisterFourLightEffect(
            BeatmapRuntimeContext context,
            string laneType,
            out int groupId)
        {
            groupId = 987654;
            var host = new GameObject($"{laneType} lane ownership effect");
            switch (laneType)
            {
                case "Color":
                    var boost = host.AddComponent<ColorBoostEffect>();
                    var color = host.AddComponent<LightColorGroupEffect>();
                    color.Count = 4;
                    color.ColorBoostEffect = boost;
                    context.Descriptor.LightColorGroupEffectManager.IdToEffect.Add(groupId, color);
                    break;
                case "Rotation":
                    var rotation = host.AddComponent<LightRotationGroupEffect>();
                    rotation.Count = 4;
                    context.Descriptor.LightRotationGroupEffectManager.IdToEffect.Add(groupId, rotation);
                    break;
                case "Translation":
                    var translation = host.AddComponent<LightTranslationGroupEffect>();
                    translation.Count = 4;
                    context.Descriptor.LightTranslationGroupEffectManager.IdToEffect.Add(groupId, translation);
                    break;
                case "FloatFX":
                    var floatFx = host.AddComponent<FloatFxGroupEffect>();
                    floatFx.Count = 4;
                    context.Descriptor.FloatFxGroupEffectManager.IdToEffect.Add(groupId, floatFx);
                    break;
                default:
                    throw new AssertionException($"Unknown GLS lane type {laneType}.");
            }

            return host;
        }

        // UnregisterEffect removes the synthetic authoritative count before its component is destroyed.
        private static void UnregisterEffect(BeatmapRuntimeContext context, string laneType, int groupId)
        {
            switch (laneType)
            {
                case "Color":
                    context.Descriptor.LightColorGroupEffectManager.IdToEffect.Remove(groupId);
                    break;
                case "Rotation":
                    context.Descriptor.LightRotationGroupEffectManager.IdToEffect.Remove(groupId);
                    break;
                case "Translation":
                    context.Descriptor.LightTranslationGroupEffectManager.IdToEffect.Remove(groupId);
                    break;
                case "FloatFX":
                    context.Descriptor.FloatFxGroupEffectManager.IdToEffect.Remove(groupId);
                    break;
            }
        }

        // GetDisplayedLabels reads only this provider's active pool and restores visual order from lane X positions.
        private static TextMeshProUGUI[] GetDisplayedLabels(GLSEventGridProvider provider)
        {
            var labelsField = typeof(GLSEventGridProvider).GetField(
                "usedLabels",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (labelsField.GetValue(provider) as IEnumerable<TextMeshProUGUI>)
                .Where(label => label.enabled)
                .OrderBy(label => label.rectTransform.localPosition.x)
                .ToArray();
        }
    }
}
