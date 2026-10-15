using UnityEngine;

public class GridLane : GridChild
{
    [Header("Plane")] [SerializeField] public GridPlane XZ;
    [SerializeField] public GridPlane XY;
    [SerializeField] public Vector2 XYOffset = Vector2.zero;
    [SerializeField] public Vector2 XYExpand = Vector2.zero;

    [Header("Visual")] [SerializeField] private Color gridColor;
    [SerializeField] private Color xyInterfaceColor;
    [SerializeField] private Color xzInterfaceColor;

    private bool oddLaneOffset;

    public override int Lane
    {
        get => base.Lane;
        set
        {
            SetLaneNoNotify(value);
            base.Lane = value;
        }
    }

    public int Height
    {
        get => (int)Size.y;
        set
        {
            SetHeightNoNotify(value);
            Size.y = value;
            if (HasController) Controller.NotifyChanged();
        }
    }

    public float Length
    {
        get => Size.z;
        set
        {
            SetLengthNoNotify(value);
            Size.z = value;
            if (HasController) Controller.NotifyChanged();
        }
    }

    public bool OddLaneOffset
    {
        get => oddLaneOffset;
        set
        {
            if (oddLaneOffset == value) return;
            oddLaneOffset = value;
            RefreshVisual();
        }
    }

    public override void OnValidate()
    {
        if (Application.isPlaying) return;
        RefreshPosition();

        SetGridColor(gridColor);
        SetXYInterfaceColor(xyInterfaceColor);
        SetXZInterfaceColor(xzInterfaceColor);

        RefreshVisual();

        base.OnValidate();
    }

    // Extend the mesh past the logical grid edges so zooming out does not clip anti-aliasing pixels.
    private const float EdgeOverdraw = 0.4f;
    private const float EdgeOverdrawFraction = 0.1f;

    private void SetLaneNoNotify(int lane)
    {
        var xyExtent = lane + XYExpand.x;
        XY.transform.localScale = new Vector3(xyExtent, XY.transform.localScale.y, XY.transform.localScale.z);
        var xyGridScale = XY.Grid.transform.localScale;
        xyGridScale.x = xyExtent > 0 ? (xyExtent + EdgeOverdraw) / xyExtent : 1f;
        XY.Grid.transform.localScale = xyGridScale;

        XZ.transform.localScale = new Vector3(lane, XZ.transform.localScale.y, XZ.transform.localScale.z);
        var xzGridScale = XZ.Grid.transform.localScale;
        xzGridScale.x = lane > 0 ? (lane + EdgeOverdraw) / lane : 1f;
        XZ.Grid.transform.localScale = xzGridScale;

        XY.transform.localPosition = new Vector3(
            (lane / 2f) + (XYOffset.x / Scale),
            XY.transform.localPosition.y,
            XY.transform.localPosition.z);
        XZ.transform.localPosition = new Vector3(
            lane / 2f,
            XZ.transform.localPosition.y,
            XZ.transform.localPosition.z);
    }

    private void SetHeightNoNotify(int height)
    {
        XY.transform.localScale = new Vector3(
            XY.transform.localScale.x,
            height + XYExpand.y,
            XY.transform.localScale.z);
        var xyGridScale = XY.Grid.transform.localScale;
        xyGridScale.y = 1f + EdgeOverdrawFraction;
        XY.Grid.transform.localScale = xyGridScale;
        XY.transform.localPosition = new Vector3(
            XY.transform.localPosition.x,
            (height / 2f) + (XYOffset.y / Scale) + (XYExpand.y / 2f),
            XY.transform.localPosition.z);
    }

    // This applies to both front and back side by 4:1
    private void SetLengthNoNotify(float length)
    {
        var calc = length + (length / 4f);
        XZ.transform.localScale = new Vector3(
            XZ.transform.localScale.x,
            calc,
            XZ.transform.localScale.z);
        var xzGridScale = XZ.Grid.transform.localScale;
        xzGridScale.y = 1f + EdgeOverdrawFraction;
        XZ.Grid.transform.localScale = xzGridScale;
        XZ.transform.localPosition = new Vector3(
            XZ.transform.localPosition.x,
            XZ.transform.localPosition.y,
            calc * 0.3f);
    }

    protected override void SetScaleNoNotify(float s)
    {
        base.SetScaleNoNotify(s);
        XY.SetScale(Scale);
        XZ.SetScale(Scale);
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
    }

    public void SetBeatSpacing(Vector4 beatSpacing)
    {
        XZ.SetSpacing(beatSpacing);
        RefreshVisual();
    }

    public void SetBeatThickness(Vector4 beatThickness)
    {
        XZ.SetThickness(beatThickness);
        RefreshVisual();
    }

    public void SetGridVisible(bool visible)
    {
        XZ.Grid.enabled = visible;
        XY.Grid.enabled = visible;
    }

    public void SetGridColor(Color color)
    {
        gridColor = color;
        XY.SetGridColor(gridColor);
        XZ.SetGridColor(gridColor);
        RefreshVisual();
    }

    public void SetXYInterfaceColor(Color color)
    {
        xyInterfaceColor = color;
        XY.SetInterfaceColor(xyInterfaceColor);
        RefreshVisual();
    }

    public void SetXZInterfaceColor(Color color)
    {
        xzInterfaceColor = color;
        XZ.SetInterfaceColor(xzInterfaceColor);
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        var xOffset = OddLaneOffset ? BeatmapConstant.LaneSize / 2f : 0f;

        XY.SetOffset(-(Vector3)XYOffset - LocalOffset + new Vector3(xOffset - (XYExpand.x / 2f), 0f, 0f));
        XZ.SetOffset(-LocalOffset + new Vector3(xOffset, 0f, 0f));

        XY.RefreshVisual();
        XZ.RefreshVisual();
    }
}
