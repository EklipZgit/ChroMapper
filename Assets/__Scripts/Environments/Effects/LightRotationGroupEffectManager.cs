using Beatmap.Enums;
using UnityEngine;

public class LightRotationGroupEffectManager : MonoBehaviour
{
    public LightRotationGroupEffect Effect;

    private void OnValidate()
    {
        if (GetComponent<LightRotationGroupEffect>() == null) gameObject.AddComponent<LightRotationGroupEffect>();
        Effect = GetComponent<LightRotationGroupEffect>();
    }

    public virtual void Initialize(AudioTimeSyncController atsc)
    {
        Effect.Atsc = atsc;
        Effect.Initialize();
    }

    public void Register(int group, int id, Axis axis, bool mirrored, Transform tr) => Effect.Register(group, id, axis, mirrored, tr);
}
