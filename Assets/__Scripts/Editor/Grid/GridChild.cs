using UnityEngine;

public class GridChild : MonoBehaviour
{
    public virtual void OnValidate()
    {
        if (Application.isPlaying) return;
        if (!HasController)
        {
            var c = transform.root.GetComponentInChildren<GridViewController>();
            Controller = c;
        }

        localOffset.y = BeatmapConstant.YOffset + (BeatmapConstant.PlayerYOffset / 2f);
        localOffset.z = BeatmapConstant.ZOffset;
        Size.w = BeatmapConstant.LaneSize;
        SetScaleNoNotify(Scale);
        if (HasController) Controller.NotifyChanged();
    }

    public GridViewController Controller;
    protected bool HasController => Controller != null;

    /// <summary>
    ///     Flag which editing mode is allowed to view.
    /// </summary>
    public EditingMode ViewableMode
    {
        get => viewableMode;
        set
        {
            if (viewableMode == value) return;
            viewableMode = value;
            if (HasController) Controller.NotifyChanged();
        }
    }

    [SerializeField] private EditingMode viewableMode = (EditingMode)byte.MaxValue;

    public bool Hide
    {
        get => hide;
        set
        {
            if (hide == value) return;
            hide = value;
            if (HasController) Controller.NotifyChanged();
        }
    }

    [SerializeField] private bool hide;

    public int Order
    {
        get => order;
        set
        {
            if (order == value) return;
            order = value;
            if (HasController) Controller.NotifyChanged();
        }
    }

    [SerializeField] private int order;

    public virtual Vector3 LocalOffset
    {
        get => localOffset;
        set
        {
            if (localOffset == value) return;
            localOffset = value;
            if (HasController) Controller.NotifyChanged();
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
            if (HasController) Controller.NotifyChanged();
        }
    }

    public float Scale
    {
        get => Size.w;
        set
        {
            if (Mathf.Approximately(Size.w, value)) return;
            Size.w = value;
            if (HasController) Controller.NotifyChanged();
        }
    }

    [SerializeField] protected Vector4 Size = Vector4.one;

    protected virtual void SetScaleNoNotify(float s) => transform.localScale = new Vector3(s, s, s);
}
