using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public static class V3FloatFxEvent
    {
        public static BaseFxEventFloat GetFromJson(JSONNode node)
        {
            var floatFxEventBase = new BaseFxEventFloat();
            
            floatFxEventBase.JsonTime = node["b"].AsFloat;
            floatFxEventBase.UsePrevious = node["p"].AsInt;
            floatFxEventBase.Value = node["v"].AsFloat;
            floatFxEventBase.Easing = node["i"].AsInt;

            return floatFxEventBase;
        }

        public static JSONNode ToJson(BaseFxEventFloat baseFxEventFloat)
        {
            return new JSONObject
            {
                ["b"] = baseFxEventFloat.JsonTime,
                ["p"] = baseFxEventFloat.UsePrevious,
                ["v"] = baseFxEventFloat.Value,
                ["i"] = baseFxEventFloat.Easing
            };
        }
    }
}
