using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Info;
using Beatmap.V3;
using Beatmap.V3.Customs;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;
// Select NUnit's generated test cases rather than Unity's identically named inspector attribute.
using Range = NUnit.Framework.RangeAttribute;

namespace TestsEditMode
{
    // The fixture covers requirements across multiple mods, so its name must not imply Heck-only coverage.
    public class ModRequirementsTest
    {
        // For use in PlayMode
        public void TestEverything()
        {
        }

        private BaseDifficulty _difficulty;
        private InfoDifficulty _infoDifficulty;

        private HeckRequirementCheck _chromaReq, _noodleReq;
        private RequirementCheck _beatToTheFutureReq, _chromaGLSReq;

        [OneTimeSetUp]
        public void SetupReqs()
        {
            _chromaReq = new ChromaReq();
            _noodleReq = new NoodleExtensionsReq();
            _beatToTheFutureReq = new BeatToTheFutureReq();
            _chromaGLSReq = new ChromaGLSReq();
        }

        [SetUp]
        public void SetupMop()
        {
            Settings.Instance.MapVersion = 3;
            _difficulty = new BaseDifficulty();
            _infoDifficulty = new InfoDifficulty(new InfoDifficultySet());
        }

        [Test]
        public void UnusedTracksDoNotRequireMods()
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["track"] = "I am unused"
                    }
                }
            };
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = "AnimateTrack",
                    Data = new JSONObject
                    {
                        ["track"] = "1",
                        ["color"] = 0,
                        ["dissolve"] = 0,
                    }
                },
                new BaseCustomEvent
                {
                    Type = "AssignPathAnimation",
                    Data = new JSONObject
                    {
                        ["track"] = "2",
                        ["color"] = 0,
                        ["dissolve"] = 0,
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.None, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void GLSCustomColorsSuggestChromaGLSInsteadOfChroma()
        {
            _difficulty.LightColorEventBoxGroups = new List<BaseLightColorEventBoxGroup>
            {
                new()
                {
                    Boxes = new List<BaseLightColorEventBox>
                    {
                        new()
                        {
                            Events = new[]
                            {
                                new BaseLightColorBase
                                {
                                    CustomData = new JSONObject
                                    {
                                        ["color"] = CreateColorArray()
                                    }
                                }
                            }
                        }
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.None,
                _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // ExtendedGLSNodeEasingsRequireChromaGLSWhenSerialized covers every new family/direction on all four
        // node categories, including V3 color's numeric customData.easing extension.
        [Test]
        public void ExtendedGLSNodeEasingsRequireChromaGLSWhenSerialized(
            [Values(3, 4)] int version,
            [Values("Color", "Rotation", "Translation", "FloatFX")] string kind,
            [Range(4, 18)] int easing)
        {
            Settings.Instance.MapVersion = version;
            AddGLSRequirementBox(kind, easing);

            Assert.AreEqual(RequirementCheck.RequirementType.Requirement,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // ExtendedGLSDistributionEasingsRequireChromaGLS protects the box-only path: the game consumes
        // distribution easing through the same converter even when every child node uses vanilla easing.
        [Test]
        public void ExtendedGLSDistributionEasingsRequireChromaGLS(
            [Values(3, 4)] int version,
            [Values("Color", "Rotation", "Translation", "FloatFX")] string kind,
            [Range(4, 18)] int easing)
        {
            Settings.Instance.MapVersion = version;
            AddGLSRequirementBox(kind, distributionEasing: easing);

            Assert.AreEqual(RequirementCheck.RequirementType.Requirement,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // VanillaGLSEasingRequirementsRespectV3ColorSchema keeps OEM curves mod-free except on V3 color,
        // where every non-native known curve is serialized through the ChromaGLS custom easing extension.
        [Test]
        public void VanillaGLSEasingRequirementsRespectV3ColorSchema(
            [Values(3, 4)] int version,
            [Values("Color", "Rotation", "Translation", "FloatFX")] string kind,
            [Values(-2, -1, 0, 1, 2, 3, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
                31, 99, 100, 101, 102, 103)] int easing)
        {
            Settings.Instance.MapVersion = version;
            AddGLSRequirementBox(kind, easing, easing);

            var usesV3ColorExtension = version == 3 && kind == "Color"
                && (easing >= 1 && easing <= 30 || easing >= 100 && easing <= 102);
            Assert.AreEqual(usesV3ColorExtension
                    ? RequirementCheck.RequirementType.Requirement
                    : RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // ColorEasingSerializationDeterminesChromaGLSRequirement requires the plugin for both the V3
        // custom extension and V4's native numeric field, not merely for selecting a curve in the UI.
        [Test]
        public void ColorEasingSerializationDeterminesChromaGLSRequirement([Range(4, 18)] int easing)
        {
            var box = (BaseLightColorEventBox)AddGLSRequirementBox("Color", easing);
            var color = box.Events[0];
            var v3Color = V3LightColorBase.GetFromJson(V3LightColorBase.ToJson(color));
            Assert.AreEqual(easing, v3Color.Easing);
            Assert.AreEqual(RequirementCheck.RequirementType.Requirement,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));

            Settings.Instance.MapVersion = 4;
            var serialized = Beatmap.V4.V4CommonData.LightColorEvent.FromBaseLightColorEvent(color).ToJson();
            var v4Color = Beatmap.V4.V4CommonData.LightColorEvent.GetFromJson(serialized);
            Assert.AreEqual(easing, v4Color.Easing);
            Assert.AreEqual(RequirementCheck.RequirementType.Requirement,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // V3ColorUsePreviousCustomEasingRequiresChromaGLS preserves the independent Extend flag while
        // still declaring the plugin needed to retain/apply the node's selected non-native curve.
        [Test]
        public void V3ColorUsePreviousCustomEasingRequiresChromaGLS([Values(0, 1)] int usePrevious)
        {
            var box = (BaseLightColorEventBox)AddGLSRequirementBox("Color", (int)EaseType.InCircular);
            box.Events[0].UsePrevious = usePrevious;
            Assert.AreEqual(RequirementCheck.RequirementType.Requirement,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));

            box.Events[0].Easing = (int)EaseType.Linear;
            Assert.AreEqual(RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // ExtendedGLSEasingRequirementTracksEditsAndRemoval exercises the actual save-time refresh,
        // including stale duplicate declarations, node deletion, and changing distribution back to vanilla.
        [Test]
        public void ExtendedGLSEasingRequirementTracksEditsAndRemoval(
            [Values(3, 4)] int version,
            [Values("Color", "Rotation", "Translation", "FloatFX")] string kind,
            [Values(false, true)] bool distribution)
        {
            Settings.Instance.MapVersion = version;
            _infoDifficulty.CustomRequirements.Add("ChromaGLS");
            _infoDifficulty.CustomRequirements.Add("ChromaGLS");
            _infoDifficulty.CustomSuggestions.Add("ChromaGLS");
            _infoDifficulty.CustomRequirements.Add("UnrelatedPlugin");
            RefreshChromaGLSRequirements();
            Assert.That(_infoDifficulty.CustomRequirements, Does.Not.Contain("ChromaGLS"));
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Not.Contain("ChromaGLS"));

            var box = AddGLSRequirementBox(kind, distribution ? 0 : 4, distribution ? 4 : 0);
            RefreshChromaGLSRequirements();
            Assert.That(_infoDifficulty.CustomRequirements, Does.Contain("ChromaGLS"));
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Not.Contain("ChromaGLS"));

            if (distribution)
                box.Easing = (int)EaseType.Linear;
            else
                box.ClearEvents();

            RefreshChromaGLSRequirements();
            Assert.That(_infoDifficulty.CustomRequirements, Does.Not.Contain("ChromaGLS"));
            Assert.That(_infoDifficulty.CustomSuggestions, Does.Not.Contain("ChromaGLS"));
            Assert.That(_infoDifficulty.CustomRequirements, Does.Contain("UnrelatedPlugin"));
        }

        // ExtendedGLSEasingRequirementOverridesSuggestions verifies that required easing wins over both
        // existing suggestions and that removing that easing restores, rather than erases, the suggestion.
        [Test]
        public void ExtendedGLSEasingRequirementOverridesSuggestions([Values(false, true)] bool ringZoom)
        {
            TracksDefinitionSO tracksDefinition = null;
            try
            {
                if (ringZoom)
                {
                    tracksDefinition = ScriptableObject.CreateInstance<TracksDefinitionSO>();
                    tracksDefinition.Register(new TrackDefinitionBasic
                    {
                        Type = 9,
                        Components = BasicEventComponent.SmoothStepRingZoom
                    });
                    _difficulty.RuntimeTracksDefinition = tracksDefinition;
                    _difficulty.Events.Add(new BaseEvent { Type = 9, CustomStep = 1.5f });
                }
                else
                {
                    var colorBox = (BaseLightColorEventBox)AddGLSRequirementBox("Color");
                    colorBox.Events[0].CustomData = new JSONObject { ["color"] = CreateColorArray() };
                }

                RefreshChromaGLSRequirements();
                Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("ChromaGLS"));
                var box = AddGLSRequirementBox("Translation", (int)EaseType.InOutExponential);
                RefreshChromaGLSRequirements();
                Assert.That(_infoDifficulty.CustomRequirements, Does.Contain("ChromaGLS"));
                Assert.That(_infoDifficulty.CustomSuggestions, Does.Not.Contain("ChromaGLS"));

                box.ClearEvents();
                RefreshChromaGLSRequirements();
                Assert.That(_infoDifficulty.CustomRequirements, Does.Not.Contain("ChromaGLS"));
                Assert.That(_infoDifficulty.CustomSuggestions, Does.Contain("ChromaGLS"));
            }
            finally
            {
                _difficulty.RuntimeTracksDefinition = null;
                if (tracksDefinition != null)
                    Object.DestroyImmediate(tracksDefinition);
            }
        }

        // UnreferencedExtendedEasingDoesNotRequireChromaGLS keeps placement/default and unused FloatFX
        // pool data out of detection; only GLS groups written into the beatmap should declare the mod.
        [Test]
        public void UnreferencedExtendedEasingDoesNotRequireChromaGLS()
        {
            Settings.Instance.MapVersion = 4;
            var box = AddGLSRequirementBox("FloatFX", (int)EaseType.InCubic);
            _difficulty.FxEventsCollection.FloatFxEvents = ((BaseVfxEventEventBox)box).Events;
            _difficulty.VfxEventBoxGroups.Clear();
            _difficulty.NJSEvents.Add(new BaseNJSEvent { Easing = (int)EaseType.InCubic });

            Assert.AreEqual(RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // V2OmittedGLSEasingsDoNotRequireChromaGLS prevents unsaved GLS data from declaring a requirement
        // after switching to the older format, which has no GLS event-box schema at all.
        [Test]
        public void V2OmittedGLSEasingsDoNotRequireChromaGLS()
        {
            Settings.Instance.MapVersion = 2;
            AddGLSRequirementBox("Color", 4, 4);
            AddGLSRequirementBox("Rotation", 7, 7);
            AddGLSRequirementBox("Translation", 10, 10);
            AddGLSRequirementBox("FloatFX", 18, 18);

            Assert.AreEqual(RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // AddGLSRequirementBox attaches only authoritative typed beatmap data so the regression suite
        // never depends on loaded visuals, the selected easing menu, or an event-order cache refresh.
        private BaseEventBox AddGLSRequirementBox(string kind, int nodeEasing = 0, int distributionEasing = 0)
        {
            BaseEventBox box;
            switch (kind)
            {
                case "Color":
                    var colorBox = new BaseLightColorEventBox
                    {
                        Events = new[] { new BaseLightColorBase { Easing = nodeEasing } }
                    };
                    _difficulty.LightColorEventBoxGroups.Add(new BaseLightColorEventBoxGroup
                    {
                        Boxes = new List<BaseLightColorEventBox> { colorBox }
                    });
                    box = colorBox;
                    break;
                case "Rotation":
                    var rotationBox = new BaseLightRotationEventBox
                    {
                        Events = new[] { new BaseLightRotationBase { EaseType = nodeEasing } }
                    };
                    _difficulty.LightRotationEventBoxGroups.Add(new BaseLightRotationEventBoxGroup
                    {
                        Boxes = new List<BaseLightRotationEventBox> { rotationBox }
                    });
                    box = rotationBox;
                    break;
                case "Translation":
                    var translationBox = new BaseLightTranslationEventBox
                    {
                        Events = new[] { new BaseLightTranslationBase { EaseType = nodeEasing } }
                    };
                    _difficulty.LightTranslationEventBoxGroups.Add(new BaseLightTranslationEventBoxGroup
                    {
                        Boxes = new List<BaseLightTranslationEventBox> { translationBox }
                    });
                    box = translationBox;
                    break;
                default:
                    Assert.AreEqual("FloatFX", kind);
                    var fxBox = new BaseVfxEventEventBox
                    {
                        Events = new[] { new BaseFxEventFloat { Easing = nodeEasing } }
                    };
                    _difficulty.VfxEventBoxGroups.Add(new BaseVfxEventEventBoxGroup
                    {
                        Boxes = new List<BaseVfxEventEventBox> { fxBox }
                    });
                    box = fxBox;
                    break;
            }

            box.Easing = distributionEasing;
            return box;
        }

        // RefreshChromaGLSRequirements isolates only the check under test while exercising the production
        // save-list update; restoring the shared registry/settings avoids leaking state into other fixtures.
        private void RefreshChromaGLSRequirements()
        {
            var originalAutomatic = Settings.Instance.AutomaticModRequirements;
            var originalChecks = new HashSet<RequirementCheck>(RequirementCheck.requirementsAndSuggestions);
            try
            {
                Settings.Instance.AutomaticModRequirements = true;
                RequirementCheck.requirementsAndSuggestions.Clear();
                RequirementCheck.RegisterRequirement(_chromaGLSReq);
                _infoDifficulty.RefreshRequirementsAndWarnings(_difficulty);
            }
            finally
            {
                Settings.Instance.AutomaticModRequirements = originalAutomatic;
                RequirementCheck.requirementsAndSuggestions.Clear();
                RequirementCheck.requirementsAndSuggestions.UnionWith(originalChecks);
            }
        }

        [Test]
        public void PreservedV3VNJSRequiresBeatToTheFuture()
        {
            // Flat VNJS has no OEM V3 runtime path, so preserving even one event must require BeatToTheFuture.
            _difficulty.NJSEvents = new List<BaseNJSEvent> { new() };

            Assert.AreEqual(
                RequirementCheck.RequirementType.Requirement,
                _beatToTheFutureReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            // Moving the runtime owner must not leave the same VNJS data attached to ChromaGLS as well.
            Assert.AreEqual(
                RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void OmittedV3VNJSDoesNotRequireBeatToTheFuture()
        {
            // The converter's No path does not write VNJS, so those in-memory events alone must not declare an unnecessary requirement.
            _difficulty.SaveVNJSEventsInV3 = false;
            _difficulty.NJSEvents = new List<BaseNJSEvent> { new() };

            Assert.AreEqual(
                RequirementCheck.RequirementType.None,
                _beatToTheFutureReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // V3 saves containing raw V4 y=3/4 walls need BeatToTheFuture even without VNJS, so automatic requirement
        // refresh must tag BeatToTheFuture and must not fall back to ChromaGLS or Mapping Extensions.
        [TestCase(3)]
        [TestCase(4)]
        public void V3UpperLaneWallsRequireBeatToTheFutureOnSave(int posY)
        {
            _difficulty.Obstacles = new List<BaseObstacle>
            {
                new()
                {
                    PosY = posY,
                    Width = 1,
                    Height = 1
                }
            };

            Assert.AreEqual(
                RequirementCheck.RequirementType.Requirement,
                _beatToTheFutureReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(
                RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(
                RequirementCheck.RequirementType.None,
                new MappingExtensionsReq().IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void SmoothStepRingZoomCustomStepSuggestsChromaGLS()
        {
            // Requirement detection uses the same component metadata as Basic Event editing.
            var trackDefinitions = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            trackDefinitions.Register(
                new TrackDefinitionBasic
                {
                    Type = (int)EventTypeValue.Event9,
                    Components = BasicEventComponent.SmoothStepRingZoom
                });
            _difficulty.RuntimeTrackDefinitions = trackDefinitions;
            _difficulty.Events = new List<BaseEvent>
            {
                new()
                {
                    Type = (int)EventTypeValue.Event9,
                    CustomStep = 1.5f
                }
            };

            Assert.AreEqual(
                RequirementCheck.RequirementType.Suggestion,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [TestCase(BasicEventComponent.SmoothStepRingZoom, false)]
        [TestCase(BasicEventComponent.RingZoom, true)]
        public void OtherRingZoomDataDoesNotSuggestChromaGLS(BasicEventComponent component, bool hasCustomStep)
        {
            // Only a custom step on the distinct smooth-step component belongs to ChromaGLS.
            var trackDefinitions = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            trackDefinitions.Register(new TrackDefinitionBasic { Type = 9, Components = component });
            _difficulty.RuntimeTrackDefinitions = trackDefinitions;
            _difficulty.Events = new List<BaseEvent>
            {
                new()
                {
                    Type = 9,
                    CustomStep = hasCustomStep ? 1.5f : null
                }
            };

            Assert.AreEqual(
                RequirementCheck.RequirementType.None,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void GLSAndBasicEventCustomColorsDeclareBothChromaMods()
        {
            _difficulty.Events = new List<BaseEvent>
            {
                new()
                {
                    Type = (int)EventTypeValue.Event0,
                    CustomData = new JSONObject
                    {
                        ["color"] = CreateColorArray()
                    }
                }
            };
            _difficulty.LightColorEventBoxGroups = new List<BaseLightColorEventBoxGroup>
            {
                new()
                {
                    Boxes = new List<BaseLightColorEventBox>
                    {
                        new()
                        {
                            Events = new[]
                            {
                                new BaseLightColorBase
                                {
                                    CustomData = new JSONObject
                                    {
                                        ["color"] = CreateColorArray()
                                    }
                                }
                            }
                        }
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion,
                _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion,
                _chromaGLSReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }


        [TestCase("AnimateComponent")]
        public void TrackTypeAlwaysRequiresOnlyChroma(string trackType)
        {
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = trackType,
                    Data = new JSONObject
                    {
                        ["track"] = "3",
                        ["dissolve"] = 0
                    }
                }
            };

            Assert.AreNotEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.None, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [TestCase("AssignTrackParent")]
        [TestCase("AssignPlayerToTrack")]
        public void TrackTypeAlwaysRequiresOnlyNoodle(string trackType)
        {
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = trackType,
                    Data = new JSONObject
                    {
                        ["track"] = "3",
                        ["color"] = 0
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.Requirement, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }
        
        [Test]
        public void AssignTrackParentAlwaysRequiresNoodle()
        {
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = "AssignTrackParent",
                    Data = new JSONObject
                    {
                        ["parentTrack"] = "parent",
                        ["childrenTracks"] = new JSONArray
                        {
                            [0] = "child"
                        }
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.Requirement, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [TestCase("position", 0)]
        [TestCase("dissolve", 1)]
        [TestCase("interactable", 1)]
        public void TrackWithUsedNoodlePropertyRequiresNoodle(string property, dynamic value)
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["track"] = "3"
                    }
                }
            };
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = "AnimateTrack",
                    Data = new JSONObject
                    {
                        ["track"] = "3",
                        [property] = value
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.Requirement, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [TestCase("color", 0)]
        public void TrackWithUsedChromaPropertySuggestsChroma(string property, dynamic value)
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["track"] = "3"
                    }
                }
            };
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = "AnimateTrack",
                    Data = new JSONObject
                    {
                        ["track"] = "3",
                        [property] = value
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.None, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [TestCase("hafsdhklsdf", 0)]
        public void TrackWithGarbagePropertyRequiresNothing(string property, dynamic value)
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["track"] = "3"
                    }
                }
            };
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = "AnimateTrack",
                    Data = new JSONObject
                    {
                        ["track"] = "3",
                        [property] = value
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.None, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void TrackWithArrayWorks()
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["track"] = new JSONArray { [0] = "2", [1] = "3" }
                    }
                }
            };
            _difficulty.CustomEvents = new List<BaseCustomEvent>
            {
                new BaseCustomEvent
                {
                    Type = "AnimateTrack",
                    Data = new JSONObject
                    {
                        ["track"] = "3",
                        ["color"] = 0,
                        ["dissolve"] = 0
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.Requirement, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [Test]
        public void NoteWithColorAnimationSuggestsChroma()
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["animation"] = new JSONObject
                        {
                            ["color"] = 0
                        }
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.Suggestion, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.None, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        [TestCase("position", 0)]
        [TestCase("dissolve", 1)]
        [TestCase("interactable", 1)]
        public void NoteWithGameplayAnimationRequiresNoodle(string property, dynamic value)
        {
            _difficulty.Notes = new List<BaseNote>
            {
                new BaseNote
                {
                    CustomData = new JSONObject
                    {
                        ["animation"] = new JSONObject
                        {
                            [property] = value
                        }
                    }
                }
            };

            Assert.AreEqual(RequirementCheck.RequirementType.None, _chromaReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
            Assert.AreEqual(RequirementCheck.RequirementType.Requirement, _noodleReq.IsRequiredOrSuggested(_infoDifficulty, _difficulty));
        }

        // SimpleJSON JSONArray requires values to be appended through Add rather than a collection initializer.
        private static JSONArray CreateColorArray()
        {
            var color = new JSONArray();
            color.Add(1f);
            color.Add(0f);
            color.Add(0f);
            return color;
        }
    }
}
