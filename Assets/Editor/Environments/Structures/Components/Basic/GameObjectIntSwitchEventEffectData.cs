using System.Linq;
using UnityEngine;

public class GameObjectIntSwitchEventEffectData : EnvironmentComponentData<GameObjectIntSwitch>
{
    public string EventType;
    public int DefaultValue;
    public GameObjectsValue[] GameObjectsValueLists;

    public class GameObjectsValue
    {
        public int Value;
        public string[] GameObjectIds;
    }

    public override void SearchAndFillComponents(GameObject self, GameObjectIntSwitch comp, CreateContainer container)
    {
        comp.GameObjectsValueContainers =
            GameObjectsValueLists
                .Select(x => new GameObjectIntSwitch.GameObjectsValueContainer
                {
                    Value = x.Value,
                    GameObjects =
                        x
                            .GameObjectIds.Select(x => container.GetGameObjectOrNull(x, self))
                            .Where(y => y != null)
                            .Select(g =>
                            {
                                g.GetComponent<ChromaIDMarker>().MarkUse = true;
                                g.GetComponent<ChromaIDMarker>().MarkActivator = true;
                                return g;
                            })
                            .ToArray()
                })
                .ToArray();
    }

    public override void CopyTo(GameObjectIntSwitch comp) => comp.DefaultValue = DefaultValue;
}
