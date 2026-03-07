using System.Linq;
using UnityEngine;

public class VisualController : MonoBehaviour
{
    public MaterialPropertyBlockController MpbController;

    public virtual void OnValidate()
    {
        if (Application.isPlaying) return;
    }
}
