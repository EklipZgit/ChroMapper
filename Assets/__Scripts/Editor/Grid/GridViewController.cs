using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class GridViewController : MonoBehaviour, IEnumerable<GridChild>
{
    public event Action OnGridViewUpdated;
    public event Action<GridChild> OnGridAdded;

    [SerializeField] private EditModeContext editModeContext;
    [SerializeField] private GridChild[] startUpRegister;

    private Dictionary<int, List<GridChild>> allChildren = new();

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        // just a neat trick
        // startUpRegister = transform.root.GetComponentsInChildren<GridChild>(true);
        allChildren.Clear();
        Start();
        NotifyChanged();
    }

    private void Start()
    {
        foreach (var gridChild in startUpRegister) RegisterChild(gridChild);
    }

    private void OnEnable() => editModeContext.OnEditModeChanged += HandleEditModeChanged;
    private void OnDestroy() => editModeContext.OnEditModeChanged -= HandleEditModeChanged;

    private void HandleEditModeChanged(EditingMode mode) => NotifyChanged();

    // TODO: Refresh only once per frame
    private void UpdateGrid()
    {
        var activeChildren = new Dictionary<int, List<GridChild>>();

        foreach (var (order, children) in allChildren)
        {
            foreach (var child in children)
            {
                if (child.ViewableMode.HasFlag(editModeContext.EditingMode) && !child.Hide)
                {
                    if (activeChildren.ContainsKey(order))
                        activeChildren[order].Add(child);
                    else
                        activeChildren.Add(order, new List<GridChild> { child });
                    child.gameObject.SetActive(true);
                }
                else
                    child.gameObject.SetActive(false);
            }
        }

        float childX = 0;
        if (activeChildren.Any(x => x.Key < 0))
        {
            if (activeChildren.TryGetValue(0, out var centerGridChildren))
                childX -= centerGridChildren.Max(x => x.Lane) / 2f;
            foreach (var (_, child) in activeChildren.Where(x => x.Key < 0))
                childX -= Mathf.Ceil(child.Max(x => x.Lane)) + 1;
        }

        var isOdd = false;
        if (activeChildren.TryGetValue(0, out var centerGrid)) isOdd = centerGrid.Max(x => x.Lane) % 2 != 0;

        foreach (var (_, children) in activeChildren)
        {
            children.RemoveAll(x => x == null);
            foreach (var child in children)
            {
                if (child is GridLane lane) lane.OddLaneOffset = isOdd;
                var xPos = childX + child.LocalOffset.x;
                child.transform.localPosition = new Vector3(
                    xPos * BeatmapConstant.LaneSize,
                    child.LocalOffset.y,
                    child.LocalOffset.z);
            }

            childX += Mathf.Ceil(children.Any() ? children.Max(x => x.Lane) + 1 : 0);
        }
    }

    public int GetSizeForOrder(int order)
    {
        return allChildren.TryGetValue(order, out var children)
            ? Mathf.CeilToInt(
                children.Any()
                    ? children
                        .Where(x => x.ViewableMode.HasFlag(editModeContext.EditingMode) && !x.Hide)
                        .Max(x => x.Lane)
                    : 0)
            : 0;
    }

    public void RegisterChild(GridChild child)
    {
        if (allChildren.TryGetValue(child.Order, out var grids))
            grids.Add(child);
        else
            allChildren[child.Order] = new List<GridChild> { child };
        OnGridAdded?.Invoke(child);
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
