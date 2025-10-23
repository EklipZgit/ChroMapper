using UnityEngine;

public class GridLane : GridChild
{
    [Header("Grid Visual")] [SerializeField]
    public GridXZ XZ;

    [SerializeField] public GridXY XY;
    [SerializeField] public Vector2 XYOffset = Vector2.zero;

    private MaterialPropertyBlock materialPropertyBlock;
    private static readonly int gridOffsetID = Shader.PropertyToID("_GridOffset");

    public override void OnValidate()
    {
        base.OnValidate();
        SetLaneNoNotify(Lane);
        SetHeightNoNotify(Height);
        SetLengthNoNotify(Length);
        MoveXYGridByZ(0f);
    }

    public void Start()
    {
        materialPropertyBlock = new MaterialPropertyBlock();
    }

    public override int Lane
    {
        get
        {
            return base.Lane;
        }
        set
        {
            SetLaneNoNotify(value);
            base.Lane = value;
        }
    }

    public int Height
    {
        get
        {
            return (int)Size.y;
        }
        set
        {
            SetHeightNoNotify(value);
            Size.y = value;
            GridViewController.NotifyChanged();
        }
    }

    public float Length
    {
        get
        {
            return Size.z;
        }
        set
        {
            SetLengthNoNotify(value);
            Size.z = value;
            GridViewController.NotifyChanged();
        }
    }

    public void SetLane(int lane)
    {
        Lane = lane;
    }

    private void SetLaneNoNotify(int lane)
    {
        XY.transform.localScale = new Vector3(lane, XY.transform.localScale.y, XY.transform.localScale.z);
        XZ.transform.localScale = new Vector3(lane, XZ.transform.localScale.y, XZ.transform.localScale.z);

        XY.transform.localPosition = new Vector3(
            (lane / 2f) + XYOffset.x,
            XY.transform.localPosition.y,
            XY.transform.localPosition.z);
        XZ.transform.localPosition = new Vector3(
            lane / 2f,
            XZ.transform.localPosition.y,
            XZ.transform.localPosition.z);
    }

    public void SetHeight(int height)
    {
        Height = height;
    }

    private void SetHeightNoNotify(int height)
    {
        XY.transform.localScale = new Vector3(XY.transform.localScale.x, height, XY.transform.localScale.z);
        XY.transform.localPosition = new Vector3(
            XY.transform.localPosition.x,
            (height / 2f) + XYOffset.y,
            XY.transform.localPosition.z);
    }

    // This applies to both front and back side by 4:1
    public void SetLength(float length)
    {
        Length = length;
    }

    private void SetLengthNoNotify(float length)
    {
        var calc = length + (length / 4f);
        XZ.transform.localScale = new Vector3(
            XZ.transform.localScale.x,
            calc,
            XZ.transform.localScale.z);
        XZ.transform.localPosition = new Vector3(
            XZ.transform.localPosition.x,
            XZ.transform.localPosition.y,
            calc * 0.3f);
    }

    public void MoveXYGridByZ(float z)
    {
        var pos = XY.transform.localPosition;
        pos.z = z;
        XY.transform.localPosition = pos;
    }

    public void RefreshPosition()
    {
        SetLaneNoNotify(Lane);
        SetHeightNoNotify(Height);
        SetLengthNoNotify(Length);
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        materialPropertyBlock.SetVector(gridOffsetID, (Vector3)XYOffset + LocalOffset);
        XY.Grid.SetPropertyBlock(materialPropertyBlock);
    }
}
