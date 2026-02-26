using UnityEngine;

[ExecuteAlways]
public class GridChild : MonoBehaviour
{
    public bool RegisterChildOnStart = true;

    public virtual void OnValidate()
    {
        localOffset.y = BeatmapConstant.YOffset + (BeatmapConstant.PlayerYOffset / 2f);
        localOffset.z = BeatmapConstant.ZOffset;
        Size.w = BeatmapConstant.LaneSize;
        SetScaleNoNotify(Scale);
        GridViewController.NotifyChanged();
    }

    private void OnEnable()
    {
        if (!RegisterChildOnStart) return;
        GridViewController.RegisterChild(this);
    }

    private void OnDisable() => GridViewController.DeregisterChild(this);

    /// <summary>
    ///     Flag which editing mode is allowed to view.
    /// </summary>
    public EditingMode ViewableMode
    {
        get => viewableMode;
        set
        {
            viewableMode = value;
            GridViewController.NotifyChanged();
        }
    }

    [SerializeField] private EditingMode viewableMode = (EditingMode)byte.MaxValue;

    public int Order
    {
        get => order;
        set
        {
            order = value;
            GridViewController.NotifyChanged();
        }
    }

    [SerializeField] private int order;

    public virtual Vector3 LocalOffset
    {
        get => localOffset;
        set
        {
            localOffset = value;
            GridViewController.NotifyChanged();
        }
    }

    [SerializeField] private Vector3 localOffset = Vector3.zero;

    public virtual int Lane
    {
        get => (int)Size.x;
        set
        {
            if (Mathf.Approximately(Size.x, value)) return;
            Size.x = value;
            GridViewController.NotifyChanged();
        }
    }

    public float Scale
    {
        get => Size.w;
        set
        {
            if (Mathf.Approximately(Size.w, value)) return;
            Size.w = value;
            GridViewController.NotifyChanged();
        }
    }
    
    [SerializeField] protected Vector4 Size = Vector4.one;
    
    protected virtual void SetScaleNoNotify(float s) => transform.localScale = new Vector3(s, s, s);
}
