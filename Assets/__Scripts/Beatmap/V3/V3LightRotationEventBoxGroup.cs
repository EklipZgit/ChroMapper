using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public static class V3LightRotationEventBoxGroup
    {
        public static BaseLightRotationEventBoxGroup GetFromJson(JSONNode node)
        {
            var group = new BaseLightRotationEventBoxGroup
            {
                JsonTime = node["b"].AsFloat,
                ID = node["g"].AsInt,
                Boxes = new List<BaseLightRotationEventBox>(
                    BaseItem
                        .GetRequiredNode(node, "e")
                        .AsArray.Linq
                        .Select(x => V3LightRotationEventBox.GetFromJson(x, node["b"].AsFloat))
                        .ToList()),
                CustomData = node["customData"]
            };

            return group;
        }

        public static JSONNode ToJson(BaseLightRotationEventBoxGroup group)
        {
            JSONNode node = new JSONObject();
            node["b"] = group.JsonTime;
            node["g"] = group.ID;
            var ary = new JSONArray();
            foreach (var k in group.Boxes) ary.Add(V3LightRotationEventBox.ToJson(k));
            node["e"] = ary;
            group.CustomData = group.SaveCustom();
            if (!group.CustomData.Children.Any()) return node;
            node["customData"] = group.CustomData;
            return node;
        }
    }
}
