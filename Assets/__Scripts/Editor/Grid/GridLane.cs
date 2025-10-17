using UnityEngine;

public class GridLane : GridChild
{
    [Header("Grid Visual")]
    [SerializeField] public GridXY XY;
    [SerializeField] public GridXZ XZ;

    public override void OnValidate()
    {
        base.OnValidate();
        SetWidthNoNotify(Size);
    }

    public override int Size
    {
        get => base.Size;
        set
        {
            SetWidthNoNotify(value);
            base.Size = value;
        }
    }

    public void SetWidth(int width) => Size = width;

    private void SetWidthNoNotify(int width)
    {
        XY.transform.localScale = new Vector3(width, XY.transform.localScale.y, XY.transform.localScale.z);
        XZ.transform.localScale = new Vector3(width, XZ.transform.localScale.y, XZ.transform.localScale.z);

        XY.transform.localPosition = new Vector3(
            width / 2f,
            XY.transform.localPosition.y,
            XY.transform.localPosition.z);
        XZ.transform.localPosition = new Vector3(
            width / 2f,
            XZ.transform.localPosition.y,
            XZ.transform.localPosition.z);
    }

    public void SetHeight(int height)
    {
        XY.transform.localScale = new Vector3(XY.transform.localScale.x, height, XY.transform.localScale.z);
        XY.transform.localPosition = new Vector3(
            XY.transform.localPosition.x,
            height / 2f,
            XY.transform.localPosition.z);
    }

    // This applies to both front and back side by 4:1
    public void SetLength(float length)
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
}
