using UnityEngine;

public abstract class BaseLightController : MonoBehaviour
{
    public int Type;
    public int ID;
    
    public abstract void SetColor(Color color);
}
