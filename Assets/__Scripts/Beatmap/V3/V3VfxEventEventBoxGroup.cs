using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.V4;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public static class V3VfxEventEventBoxGroup
    {
        public static BaseVfxEventEventBoxGroup GetFromJson(JSONNode node, IList<BaseFxEventFloat> floatFxEvents)
        {
            var group = new BaseVfxEventEventBoxGroup
            {
                JsonTime = node["b"].AsFloat,
                ID = node["g"].AsInt,
                Type = node["t"].AsInt,
                CustomData = node["customData"]
            };
            group.Boxes = BaseItem
                .GetRequiredNode(node, "e")
                .AsArray.Linq
                .Select((x, i) =>
                {
                    var box = V3VfxEventEventBox.GetFromJson(x.Value, floatFxEvents);
                    foreach (var evt in box.Events)
                    {
                        evt.EventBoxGroupData = group;
                        evt.EventBoxData = box;
                        evt.BoxIndex = i;
                        evt.JsonTime = group.JsonTime;
                    }

                    return box;
                })
                .ToList();

            return group;
        }

        public static JSONNode ToJson(
            BaseVfxEventEventBoxGroup vfxGroup,
            IList<BaseFxEventFloat> floatFxEvents)
        {
            JSONNode node = new JSONObject();
            node["b"] = vfxGroup.JsonTime;
            node["g"] = vfxGroup.ID;
            node["t"] = vfxGroup.Type;
            var ary = new JSONArray();
            foreach (var k in vfxGroup.Boxes) ary.Add(V3VfxEventEventBox.ToJson(k, floatFxEvents));
            node["e"] = ary;
            vfxGroup.CustomData = vfxGroup.SaveCustom();
            if (!vfxGroup.CustomData.Children.Any()) return node;
            node["customData"] = vfxGroup.CustomData;
            return node;
        }
    }
}
