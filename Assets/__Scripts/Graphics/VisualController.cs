using System.Linq;
using UnityEngine;

public class VisualController : MonoBehaviour
{
    public VisualRepositorySO Repository;
    public MaterialPropertyBlockController MpbController;

    public virtual void OnValidate()
    {
        if (Application.isPlaying) return;
        if (Repository == null) Repository = Resources.FindObjectsOfTypeAll<VisualRepositorySO>().FirstOrDefault();
    }
}
