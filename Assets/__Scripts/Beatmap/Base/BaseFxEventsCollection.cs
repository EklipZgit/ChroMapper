using System.Linq;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseFxEventsCollection : BaseItem
    {
        public BaseFxEventInt[] IntFxEvents = { };
        public BaseFxEventFloat[] FloatFxEvents = { };

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 => V3FxEventsCollection.ToJson(this)
            };


        public override BaseItem Clone()
        {
            var eventsCollection = new BaseFxEventsCollection();
            eventsCollection.IntFxEvents = IntFxEvents.Select(evt => evt.Clone() as BaseFxEventInt).ToArray();
            eventsCollection.FloatFxEvents = FloatFxEvents.Select(evt => evt.Clone() as BaseFxEventFloat).ToArray();
            return eventsCollection;
        }
    }
}
