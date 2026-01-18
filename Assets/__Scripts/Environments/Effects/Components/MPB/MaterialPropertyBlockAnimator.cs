using UnityEngine;

public class MaterialPropertyBlockAnimator : MonoBehaviour
{
    public MaterialPropertyBlockController Controller;
    public string Property;
    protected int PropertyId;

    protected virtual void SetProperty()
    {
    }

    protected void Awake()
    {
        PropertyId = Shader.PropertyToID(Property);
        enabled = Controller != null;
    }

    protected void Update()
    {
        SetProperty();
        Controller.ApplyChanges();
    }
}
