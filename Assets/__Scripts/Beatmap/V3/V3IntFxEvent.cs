using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public static class V3IntFxEvent
    {
        public static IntFxEventBase GetFromJson(JSONNode node)
        {
            var intFxEventBase = new IntFxEventBase();
            
            intFxEventBase.JsonTime = node["b"].AsFloat;
            intFxEventBase.UsePrevious = node["p"].AsInt;
            intFxEventBase.Value = node["v"].AsInt;

            return intFxEventBase;
        }

        public static JSONNode ToJson(IntFxEventBase intFxEventBase)
        {
            return new JSONObject
            {
                ["b"] = intFxEventBase.JsonTime,
                ["p"] = intFxEventBase.UsePrevious,
                ["v"] = intFxEventBase.Value
            };
        }
    }
}
