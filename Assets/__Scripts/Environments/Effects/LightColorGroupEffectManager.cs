using UnityEngine;

public class LightColorGroupEffectManager :MonoBehaviour
{
    public LightColorGroupEffect Effect;

    private void OnValidate()
    {
        if (GetComponent<LightColorGroupEffect>() == null) gameObject.AddComponent<LightColorGroupEffect>();
        Effect = GetComponent<LightColorGroupEffect>();
    }
    
    public virtual void Initialize(AudioTimeSyncController atsc, ColorSchemeSO colorScheme)
    {
        Effect.Atsc = atsc;
        Effect.ColorScheme = colorScheme;
        Effect.Initialize();
    }

    public void Register(int group, int id, LightController controllable) => Effect.Register(group, id, controllable);
}
