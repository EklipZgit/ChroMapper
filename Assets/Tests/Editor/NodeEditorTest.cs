using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    internal class NodeEditorTest : TestBase
    {
        protected override IEnumerator OnMapLoaded()
        {
            NodeEditorController.IsActive = true;
            Settings.Instance.MapVersion = 3;
            yield break;
        }

        protected override void OnReturnSettings()
        {
            NodeEditorController.IsActive = false;
        }

        [Test]
        public void JsonMerge()
        {
            Object.FindAnyObjectByType<EventPlacement>();
            var nodeEditor = Object.FindAnyObjectByType<NodeEditorController>();
            var inputField = nodeEditor.GetComponentInChildren<TMP_InputField>();

            var eventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.BackLasers,
                Value = (int)LightValue.Off,
                FloatValue = 1,
                CustomData =
                    JSON.Parse(
                        @"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""typeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]},""lenDiffer"":[1]}")
            };
            var eventB = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.LeftLasers,
                Value = (int)LightValue.Off,
                FloatValue = 1,
                CustomData =
                    JSON.Parse(
                        @"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":2,""s"":""t"",""b"":false,""a"":[2,2]},""typeDiffer"":{""i"":{},""s"":[],""o"":true,""a"":1},""lenDiffer"":[1,2]}")
            };
            var eventC = new BaseEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.RightLasers, Value = (int)LightValue.Off
            };
            eventA = PlaceUtils.Place(eventA);
            eventB = PlaceUtils.Place(eventB);
            eventC = PlaceUtils.Place(eventC);

            SelectionController.Select(eventC);
            Assert.AreEqual("{\n  \"b\" : 2,\n  \"et\" : 3,\n  \"i\" : 0,\n  \"f\" : 1\n}", inputField.text);

            SelectionController.Select(eventA);
            Assert.AreEqual(
                "{\n  \"b\" : 2,\n  \"et\" : 0,\n  \"i\" : 0,\n  \"f\" : 1,\n  \"customData\" : {\n    \"matches\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"b\" : true,\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"differs\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"b\" : true,\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"typeDiffer\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"o\" : {\n      },\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"lenDiffer\" : [\n      1\n    ]\n  }\n}",
                inputField.text);

            SelectionController.Select(eventB, true);
            Assert.AreEqual(
                "{\n  \"b\" : 2,\n  \"et\" : -,\n  \"i\" : 0,\n  \"f\" : 1,\n  \"customData\" : {\n    \"matches\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"b\" : true,\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"differs\" : {\n      \"i\" : -,\n      \"s\" : -,\n      \"b\" : -,\n      \"a\" : [\n        -,\n        2\n      ]\n    },\n    \"typeDiffer\" : {\n    }\n  }\n}",
                inputField.text);
        }

        [Test]
        public void JsonApply()
        {
            Object.FindAnyObjectByType<EventPlacement>();
            var nodeEditor = Object.FindAnyObjectByType<NodeEditorController>();
            var inputField = nodeEditor.GetComponentInChildren<TMP_InputField>();

            var eventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.BackLasers,
                Value = (int)LightValue.Off,
                FloatValue = 1f,
                CustomData =
                    JSON.Parse(
                        @"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""typeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]},""lenDiffer"":[1],""updatedLenDiffer"":[1],""updated"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""updatedDiffer"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""updatedTypeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]}}")
            };
            var eventB = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.LeftLasers,
                Value = (int)LightValue.Off,
                FloatValue = 0.5f,
                CustomData =
                    JSON.Parse(
                        @"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":2,""s"":""t"",""b"":false,""a"":[2,2]},""typeDiffer"":{""i"":{},""s"":[],""o"":true,""a"":1},""lenDiffer"":[1,2],""updatedLenDiffer"":[1,2],""updated"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""updatedDiffer"":{""i"":2,""s"":""t"",""b"":false,""a"":[2,2]},""updatedTypeDiffer"":{""i"":{},""s"":[],""o"":true,""a"":1}}")
            };
            eventA = PlaceUtils.Place(eventA);
            eventB = PlaceUtils.Place(eventB);

            SelectionController.Select(eventA);
            SelectionController.Select(eventB, true);

            nodeEditor.NodeEditor_EndEdit(
                @"{""b"": -, ""et"": -, ""i"": -, ""f"": -, ""customData"": {""matches"":{},""differs"":{},""typeDiffer"":{},""updatedLenDiffer"":[1],""updated"":{""i"":4,""s"":""q"",""b"":false,""a"":[3,2]},""updatedDiffer"":{""i"":4,""s"":""q"",""b"":false,""a"":[3,2]},""updatedTypeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]}}}");

            var selectedObjects = SelectionController.SelectedObjects.ToArray();
            Assert.AreEqual(2, selectedObjects.Length, "Exactly two objects should be selected after NodeEditor_EndEdit");
            var selectedEvents = selectedObjects.OfType<BaseEvent>().ToArray();
            Assert.AreEqual(selectedObjects.Length, selectedEvents.Length);
            foreach (var sel in selectedEvents)
            {
                BeatmapAssertion.IsInCollection(sel, $"Selected object of type {sel.ObjectType} must be present in its BeatmapObjectContainerCollection");
            }

            Assert.AreEqual(
                "{\n  \"b\" : 2,\n  \"et\" : -,\n  \"i\" : 0,\n  \"f\" : -,\n  \"customData\" : {\n    \"matches\" : {\n    },\n    \"differs\" : {\n    },\n    \"typeDiffer\" : {\n    },\n    \"updatedLenDiffer\" : [\n      1\n    ],\n    \"updated\" : {\n      \"i\" : 4,\n      \"s\" : \"q\",\n      \"b\" : false,\n      \"a\" : [\n        3,\n        2\n      ]\n    },\n    \"updatedDiffer\" : {\n      \"i\" : 4,\n      \"s\" : \"q\",\n      \"b\" : false,\n      \"a\" : [\n        3,\n        2\n      ]\n    },\n    \"updatedTypeDiffer\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"o\" : {\n      },\n      \"a\" : [\n        1,\n        2\n      ]\n    }\n  }\n}",
                inputField.text);

            // Objects have been recreated, pick them up from the selection controller
            var events = SelectionController.SelectedObjects.ToArray();
            Assert.AreEqual(
                "{\"b\":2,\"et\":0,\"i\":0,\"f\":1,\"customData\":{\"matches\":{},\"differs\":{},\"typeDiffer\":{\"i\":1,\"s\":\"s\",\"o\":{},\"a\":[1,2]},\"lenDiffer\":[1],\"updatedLenDiffer\":[1],\"updated\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedDiffer\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedTypeDiffer\":{\"i\":1,\"s\":\"s\",\"o\":{},\"a\":[1,2]}}}",
                events[0].ToJson().ToString());
            Assert.AreEqual(
                "{\"b\":2,\"et\":2,\"i\":0,\"f\":0.5,\"customData\":{\"matches\":{},\"differs\":{},\"typeDiffer\":{\"i\":{},\"s\":[],\"o\":true,\"a\":1},\"lenDiffer\":[1,2],\"updatedLenDiffer\":[1],\"updated\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedDiffer\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedTypeDiffer\":{\"i\":1,\"s\":\"s\",\"o\":{},\"a\":[1,2]}}}",
                events[1].ToJson().ToString());
        }
    }
}
