using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // TubeBloomPrePassLightWithIdData must distinguish an omitted serialized
    // reference (warn, stay null, never infer children) from an explicit integer 0
    // (null or intentionally excluded, silent) and must reject unresolvable
    // nonzero instance IDs.
    // TestsEditMode cannot see Newtonsoft.Json, so fixtures go through the production
    // CreateUtils.JsonToEnvironmentData entry point and exceptions are matched by type name.
    public class TubeBloomReferenceImportTest
    {
        private const int SpriteInstanceId = 4001;
        private const int BoxInstanceId = 4002;

        private GameObject self;
        private ParametricBloomFogLightController comp;
        private ParametricSpriteLight sprite;
        private ParametricBoxLight box;
        private CreateContainer container;

        private static readonly string CurveJson =
            "{\"keys\":[{\"time\":0,\"value\":0,\"inTangent\":0,\"outTangent\":1}," +
            "{\"time\":1,\"value\":1,\"inTangent\":1,\"outTangent\":0}]}";

        [SetUp]
        public void SetUp()
        {
            self = new GameObject("Tube");
            self.AddComponent<ChromaIDMarker>().ChromaID = "TestEnv.[0]Environment.[0]Tube";
            comp = self.AddComponent<ParametricBloomFogLightController>();

            var spriteGo = new GameObject("BakedSprite");
            spriteGo.transform.SetParent(self.transform, false);
            var spriteData = new Parametric3SliceSpriteControllerData { InstanceId = SpriteInstanceId };
            spriteData.SpawnComponent(spriteGo);
            sprite = (ParametricSpriteLight)spriteData.Instance;

            var boxGo = new GameObject("BoxLight");
            boxGo.transform.SetParent(self.transform, false);
            var boxData = new ParametricBoxControllerData { InstanceId = BoxInstanceId };
            boxData.SpawnComponent(boxGo);
            box = (ParametricBoxLight)boxData.Instance;

            container = new CreateContainer();
            container.ComponentInstances[SpriteInstanceId] = spriteData;
            container.ComponentInstances[BoxInstanceId] = boxData;
        }

        [TearDown]
        public void TearDown()
        {
            if (self != null) Object.DestroyImmediate(self);
        }

        private static string TubeJson(string referenceFields)
        {
            return "{\"lightId\":0,\"instanceId\":300,\"isEnabled\":true,\"tubeBloomPrePassLight\":{" +
                   "\"instanceId\":301,\"thickenCurve\":" + CurveJson +
                   ",\"alphaToLengthBloomFogCurve\":" + CurveJson +
                   ",\"alphaToLengthCurve\":" + CurveJson + referenceFields + "}}";
        }

        private TubeBloomPrePassLightWithIdData DeserializeTube(string tubeJson)
        {
            var json = "{\"environmentData\":{\"environmentID\":\"TestEnv\"}," +
                       "\"objects\":[{\"id\":\"TestEnv.[0]Environment.[0]Tube\"," +
                       "\"components\":{\"TubeBloomPrePassLightWithId\":[" + tubeJson + "]}}]}";
            var asset = new TextAsset(json);
            try
            {
                var data = CreateUtils.JsonToEnvironmentData(asset);
                container.Data = data;
                return data.Objects[0].Components.TubeBloomPrePassLightWithId[0];
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        private TubeBloomPrePassLightWithIdData Import(string tubeJson)
        {
            var tube = DeserializeTube(tubeJson);
            tube.FillComponents(self, comp, container);
            return tube;
        }

        private System.Exception CatchImport(string tubeJson)
        {
            return Assert.Catch(() => Import(tubeJson));
        }

        private static void AssertJsonSerializationException(System.Exception exception, params string[] expected)
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.GetType().Name, Is.EqualTo("JsonSerializationException"));
            foreach (var text in expected)
                Assert.That(exception.Message, Does.Contain(text));
        }

        private static List<string> CaptureWarnings(System.Action action)
        {
            var warnings = new List<string>();
            void Handler(string message, string stackTrace, LogType type)
            {
                if (type == LogType.Warning) warnings.Add(message);
            }

            Application.logMessageReceived += Handler;
            try
            {
                action();
            }
            finally
            {
                Application.logMessageReceived -= Handler;
            }

            return warnings;
        }

        [Test]
        public void OmittedReferencesWarnAndStayNull()
        {
            LogAssert.Expect(
                LogType.Warning,
                new Regex(Regex.Escape(
                    "Tube bloom pre-pass light 'TestEnv.[0]Environment.[0]Tube' (instance 301) " +
                    "in environment 'TestEnv' is missing exported parametricBoxController and " +
                    "dynamic3SliceSprite; emit integer 0 for a null or intentionally excluded " +
                    "reference. References are not inferred from the object hierarchy.")));

            Import(TubeJson(""));

            Assert.That(comp.BoxLight, Is.Null, "Omitted refs must not adopt hierarchy children.");
            Assert.That(comp.SpriteLight, Is.Null, "Omitted refs must not adopt hierarchy children.");
            Assert.That(comp.BloomFog, Is.Not.Null);
        }

        [Test]
        public void ExplicitZeroReferencesStayNullWithoutWarning()
        {
            var warnings = CaptureWarnings(() =>
                Import(TubeJson(",\"parametricBoxController\":0,\"dynamic3SliceSprite\":0")));

            Assert.That(warnings, Is.Empty,
                "An explicit integer 0 is the canonical null or intentionally-excluded reference.");
            Assert.That(comp.BoxLight, Is.Null);
            Assert.That(comp.SpriteLight, Is.Null);
        }

        [Test]
        public void MissingInstanceIdThrowsWithoutPartialMutation()
        {
            var exception = CatchImport(TubeJson(
                ",\"parametricBoxController\":0,\"dynamic3SliceSprite\":999999"));

            AssertJsonSerializationException(exception, "dynamic3SliceSprite", "999999", "TestEnv");
            Assert.That(comp.BloomFog, Is.Null, "A failed import must not leave a BloomFog component.");
            Assert.That(comp.BoxLight, Is.Null);
            Assert.That(comp.SpriteLight, Is.Null);
        }

        [Test]
        public void WrongTypeInstanceIdThrowsWithoutPartialMutation()
        {
            var exception = CatchImport(TubeJson(
                ",\"parametricBoxController\":0,\"dynamic3SliceSprite\":" + BoxInstanceId));

            AssertJsonSerializationException(exception, "dynamic3SliceSprite", BoxInstanceId.ToString());
            Assert.That(comp.BloomFog, Is.Null, "A failed import must not leave a BloomFog component.");
            Assert.That(comp.BoxLight, Is.Null);
            Assert.That(comp.SpriteLight, Is.Null);
        }

        [Test]
        public void MissingBoxInstanceIdThrowsWithoutPartialMutation()
        {
            var exception = CatchImport(TubeJson(
                ",\"parametricBoxController\":999999,\"dynamic3SliceSprite\":0"));

            AssertJsonSerializationException(exception, "parametricBoxController", "999999", "TestEnv");
            Assert.That(comp.BloomFog, Is.Null, "A failed import must not leave a BloomFog component.");
            Assert.That(comp.BoxLight, Is.Null);
            Assert.That(comp.SpriteLight, Is.Null);
        }

        [Test]
        public void MissingSpriteOnlyWarnsNamingOnlyThatField()
        {
            var warnings = CaptureWarnings(() =>
                Import(TubeJson(",\"parametricBoxController\":0")));

            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("dynamic3SliceSprite"));
            Assert.That(warnings[0], Does.Not.Contain("parametricBoxController"));
            Assert.That(comp.SpriteLight, Is.Null);
            Assert.That(comp.BoxLight, Is.Null);
        }

        [Test]
        public void MissingBoxOnlyWarnsNamingOnlyThatField()
        {
            var warnings = CaptureWarnings(() =>
                Import(TubeJson(",\"dynamic3SliceSprite\":0")));

            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("parametricBoxController"));
            Assert.That(warnings[0], Does.Not.Contain("dynamic3SliceSprite"));
            Assert.That(comp.BoxLight, Is.Null);
            Assert.That(comp.SpriteLight, Is.Null);
        }

        [Test]
        public void JsonNullReferencesWarnLikeOmitted()
        {
            var warnings = CaptureWarnings(() =>
                Import(TubeJson(",\"parametricBoxController\":null,\"dynamic3SliceSprite\":null")));

            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("parametricBoxController"));
            Assert.That(warnings[0], Does.Contain("dynamic3SliceSprite"));
            Assert.That(comp.BoxLight, Is.Null);
            Assert.That(comp.SpriteLight, Is.Null);
        }

        [Test]
        public void EachGenerationPassWarnsAgain()
        {
            var tube = DeserializeTube(TubeJson(""));

            var warnings = CaptureWarnings(() =>
            {
                tube.FillComponents(self, comp, container);
                tube.FillComponents(self, comp, container);
            });

            Assert.That(warnings, Has.Count.EqualTo(2),
                "Every generation pass must surface the missing export again.");
        }

        [Test]
        public void ValidReferencesResolveToExportedComponents()
        {
            Import(TubeJson(
                ",\"parametricBoxController\":" + BoxInstanceId +
                ",\"dynamic3SliceSprite\":" + SpriteInstanceId));

            Assert.That(comp.BoxLight, Is.SameAs(box));
            Assert.That(comp.SpriteLight, Is.SameAs(sprite));
            Assert.That(comp.BloomFog, Is.Not.Null);
        }

        [Test]
        public void MissingTubeObjectThrows()
        {
            var json = "{\"environmentData\":{\"environmentID\":\"TestEnv\"}," +
                       "\"objects\":[{\"id\":\"TestEnv.[0]Environment.[0]Tube\"," +
                       "\"components\":{\"TubeBloomPrePassLightWithId\":[{\"lightId\":0," +
                       "\"instanceId\":300,\"isEnabled\":true}]}}]}";
            var asset = new TextAsset(json);
            try
            {
                var data = CreateUtils.JsonToEnvironmentData(asset);
                container.Data = data;
                AssertJsonSerializationException(
                    Assert.Catch(() => data.Objects[0].Components.TubeBloomPrePassLightWithId[0]
                        .FillComponents(self, comp, container)),
                    "tubeBloomPrePassLight");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
