using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ZLinq;
using UnityEngine;

[ExecuteAlways]
public class GridViewController : MonoBehaviour, IEnumerable<GridChild>
{
    public event Action OnGridViewUpdated;

    [SerializeField] private GridRenderingController gridRenderingController;
    [SerializeField] private EditModeContext editModeContext;
    [SerializeField] private GridChild[] startUpRegister;

    private Dictionary<int, List<GridChild>> allChildren = new();
    private readonly Dictionary<int, List<GridChild>> reuseChildren = new();

    private bool hasInitialized;
    private bool gridsVisible = true;
    public bool IsOdd;

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        // just a neat trick
        // startUpRegister = transform.root.GetComponentsInChildren<GridChild>(true);
        allChildren.Clear();
        Start();
        NotifyChanged();
    }

    private void Start() => InitIfNeeded();

    private void OnDestroy()
    {
        editModeContext.OnEditModeChanged -= HandleEditModeChanged;
        EditorScaleController.OnEditorScaleChanged -= HandleEditorScaleChanged;
        gridRenderingController.OnBeatSpacingChanged -= HandleBeatSpacingChanged;
        gridRenderingController.OnXYInterfaceColorChanged -= HandleXYInterfaceColorChanged;
        gridRenderingController.OnXZInterfaceColorChanged -= HandleXZInterfaceColorChanged;
        gridRenderingController.OnLengthChanged -= HandleLengthChanged;
        gridRenderingController.OnBeatThicknessChanged -= HandleBeatThicknessChanged;
        UIMode.OnUIModeSwitched -= HandleUIModeChanged;
    }

    private void InitIfNeeded()
    {
        if (hasInitialized) return;
        foreach (var gridChild in startUpRegister) RegisterChild(gridChild);
        editModeContext.OnEditModeChanged += HandleEditModeChanged;
        EditorScaleController.OnEditorScaleChanged += HandleEditorScaleChanged;
        gridRenderingController.OnBeatSpacingChanged += HandleBeatSpacingChanged;
        gridRenderingController.OnXYInterfaceColorChanged += HandleXYInterfaceColorChanged;
        gridRenderingController.OnXZInterfaceColorChanged += HandleXZInterfaceColorChanged;
        gridRenderingController.OnLengthChanged += HandleLengthChanged;
        gridRenderingController.OnBeatThicknessChanged += HandleBeatThicknessChanged;
        UIMode.OnUIModeSwitched += HandleUIModeChanged;
        hasInitialized = true;
        foreach (var lane in this.Where(x => x is GridLane).Cast<GridLane>())
        {
            ApplyScale(lane, EditorScaleController.EditorScale);
            ApplyVisual(lane);
        }

        NotifyChanged();
        HandleUIModeChanged(UIMode.SelectedMode);
    }

    private void HandleEditModeChanged(EditingMode mode) => NotifyChanged();

    private void HandleEditorScaleChanged(float scale)
    {
        foreach (var lane in this.Where(x => x is GridLane).Cast<GridLane>()) ApplyScale(lane, scale);
    }

    private void HandleBeatSpacingChanged(Vector4 zLineSpacing)
    {
        foreach (var lane in this.Where(x => x is GridLane).Cast<GridLane>()) lane.SetBeatSpacing(zLineSpacing);
    }

    private void HandleXYInterfaceColorChanged(Color color)
    {
        foreach (var lane in this.Where(x => x is GridLane).Cast<GridLane>()) lane.SetXYInterfaceColor(color);
    }

    private void HandleXZInterfaceColorChanged(Color color)
    {
        foreach (var lane in this.Where(x => x is GridLane).Cast<GridLane>()) lane.SetXZInterfaceColor(color);
    }

    private void HandleLengthChanged(float scale) => HandleEditorScaleChanged(scale);

    private void HandleBeatThicknessChanged(Vector4 zLineThickness)
    {
        foreach (var lane in this.Where(x => x is GridLane).Cast<GridLane>()) lane.SetBeatThickness(zLineThickness);
    }

    private void HandleUIModeChanged(UIModeType mode)
    {
        gridsVisible = mode is UIModeType.Normal or UIModeType.HideUI;
        foreach (var lane in this.AsValueEnumerable().Where(x => x is GridLane).Cast<GridLane>())
            lane.SetGridVisible(gridsVisible);
    }

    private static void ApplyScale(GridLane lane, float scale) => lane.Length = Settings.Instance.TrackLength * scale;

    private void ApplyVisual(GridLane lane)
    {
        lane.SetBeatSpacing(gridRenderingController.ZLineSpacing);

        var newColor = Color.white.WithAlpha(Settings.Instance.InterfaceOpacity);
        lane.SetXYInterfaceColor(newColor);

        var gridAlpha = Settings.Instance.GridTransparency;
        newColor = Settings.Instance.HighContrastGrids
            ? GridRenderingController.ColorHighContrast
            : GridRenderingController.ColorDefault;
        newColor.a = Mathf.Clamp01(1f - gridAlpha);
        lane.SetXZInterfaceColor(newColor);

        lane.SetBeatThickness(gridRenderingController.ZLineThickness);
    }

    // TODO: Refresh only once per frame
    private void UpdateGrid()
    {
        reuseChildren.Clear();

        foreach (var (order, children) in allChildren)
        {
            foreach (var child in children)
            {
                if (child.ViewableMode.HasFlag(editModeContext.EditingMode) && !child.Hide)
                {
                    if (reuseChildren.TryGetValue(order, out var childList))
                        childList.Add(child);
                    else
                        reuseChildren.Add(order, new List<GridChild> { child });
                    child.gameObject.SetActive(true);
                }
                else
                    child.gameObject.SetActive(false);
            }
        }

        float childX = 0;
        if (reuseChildren.Any(x => x.Key < 0))
        {
            if (reuseChildren.TryGetValue(0, out var centerGridChildren))
                childX -= centerGridChildren.Max(x => x.Lane) / 2f;
            foreach (var (_, child) in reuseChildren.Where(x => x.Key < 0))
                childX -= Mathf.Ceil(child.Max(x => x.Lane)) + 1;
        }

        IsOdd = false;
        if (reuseChildren.TryGetValue(0, out var centerGrid)) IsOdd = centerGrid.Max(x => x.Lane) % 2 != 0;

        foreach (var (_, children) in reuseChildren)
        {
            children.RemoveAll(x => x == null);
            foreach (var child in children)
            {
                if (child is GridLane lane) lane.OddLaneOffset = IsOdd;
                var xPos = childX + child.LocalOffset.x;
                child.transform.localPosition = new Vector3(
                    xPos * BeatmapConstant.LaneSize,
                    child.LocalOffset.y,
                    child.LocalOffset.z);
            }

            childX += Mathf.Ceil(children.Any() ? children.Max(x => x.Lane) + 1 : 0);
        }
    }

    public void RegisterChild(GridChild child)
    {
        if (allChildren.TryGetValue(child.Order, out var grids))
            grids.Add(child);
        else
            allChildren[child.Order] = new List<GridChild> { child };
        if (child is GridLane lane)
        {
            ApplyScale(lane, EditorScaleController.EditorScale);
            ApplyVisual(lane);
            lane.SetGridVisible(gridsVisible);
        }

        NotifyChanged();
    }

    public void DeregisterChild(GridChild child)
    {
        if (!allChildren.TryGetValue(child.Order, out var grids)) return;
        grids.Remove(child);
        if (grids.Count != 0) return;
        allChildren.Remove(child.Order);
        NotifyChanged();
    }

    public void NotifyChanged()
    {
        RefreshChildDictionary();
        UpdateGrid();
        OnGridViewUpdated?.Invoke();
    }

    private void RefreshChildDictionary()
    {
        allChildren = allChildren
            .SelectMany(x => x.Value)
            .GroupBy(x => x.Order)
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.ToList());
    }

    public IEnumerator<GridChild> GetEnumerator()
    {
        foreach (var (_, child) in allChildren)
        foreach (var grid in child)
            yield return grid;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
