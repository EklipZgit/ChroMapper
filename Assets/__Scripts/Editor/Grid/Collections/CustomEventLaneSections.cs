using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class CustomEventLaneSections : IDisposable
{
    private sealed class SectionView
    {
        public readonly GridLane Grid;
        public readonly TextMeshProUGUI Heading;

        public SectionView(GridLane grid, TextMeshProUGUI heading)
        {
            Grid = grid;
            Heading = heading;
        }
    }

    private readonly GridLane original;
    private readonly GLSGroupTrack labelTemplate;
    private readonly int firstOrder;
    private readonly List<SectionView> views = new();

    public CustomEventLaneSections(GridLane original, GLSGroupTrack labelTemplate)
    {
        this.original = original;
        this.labelTemplate = labelTemplate;
        firstOrder = original.Order;
    }

    private SectionView CreateView(int index)
    {
        var grid = original;
        if (index > 0)
        {
            var owner = new GameObject("Custom event category lanes");
            owner.SetActive(false);
            owner.transform.SetParent(original.transform.parent, false);
            owner.transform.localScale = original.transform.localScale;
            grid = owner.AddComponent<GridLane>();
            grid.XY = UnityEngine.Object.Instantiate(original.XY, owner.transform);
            grid.XZ = UnityEngine.Object.Instantiate(original.XZ, owner.transform);
            grid.XY.gameObject.SetActive(true);
            grid.XZ.gameObject.SetActive(true);
            grid.XYOffset = original.XYOffset;
            grid.XYExpand = original.XYExpand;
            grid.ViewableMode = original.ViewableMode;
            grid.Order = firstOrder + index;
            grid.LocalOffset = original.LocalOffset;
            grid.Scale = original.Scale;
            grid.Height = original.Height;
            grid.Length = original.Length;
            grid.SetGridColor(original.GridColor);
            grid.Hide = true;
            grid.Controller = original.Controller;
            var provider = owner.AddComponent<PlacementProvider>();
            provider.Placements = original.GetComponent<PlacementProvider>().Placements;
            provider.Lane = grid;
            original.Controller.RegisterChild(grid);
        }

        var heading = labelTemplate.CreateGroupLabel(grid.transform);
        heading.name = "Custom Event Category Heading";
        heading.richText = false;
        return new SectionView(grid, heading);
    }

    public void UpdateLayout(
        IReadOnlyList<CustomEventLaneIndex.Section> sections,
        int unsplitLaneCount,
        bool split)
    {
        var hidden = !Settings.Instance.AdvancedShit;
        var activeCount = split ? sections.Count : 0;
        for (var index = 0; index < activeCount; index++)
        {
            if (index == views.Count)
                views.Add(CreateView(index));

            var view = views[index];
            var section = sections[index];
            view.Grid.Order = firstOrder + index;
            view.Grid.LeadingSpace = CustomEventLaneIndex.SectionSpacing - 1;
            view.Grid.Lane = section.LaneCount;
            view.Grid.Hide = hidden;
            if (index > 0)
                view.Grid.name = section.Type + " lanes";

            view.Heading.text = section.Type;
            view.Heading.transform.parent.gameObject.SetActive(!hidden);
        }

        for (var index = activeCount; index < views.Count; index++)
        {
            var view = views[index];
            view.Heading.transform.parent.gameObject.SetActive(false);
            if (index > 0)
                view.Grid.Hide = true;
        }

        var firstLaneCount = split
            ? sections.Count == 0
                ? 0
                : sections[0].LaneCount
            : unsplitLaneCount;
        original.Lane = firstLaneCount;
        original.LeadingSpace = split ? CustomEventLaneIndex.SectionSpacing - 1 : 0;
        original.XY.gameObject.SetActive(firstLaneCount > 0);
        original.XZ.gameObject.SetActive(firstLaneCount > 0);
        original.Hide = hidden || firstLaneCount == 0;
    }

    public void Dispose()
    {
        foreach (var view in views)
        {
            if (view.Heading != null)
                UnityEngine.Object.Destroy(view.Heading.transform.parent.gameObject);

            if (view.Grid == null || view.Grid == original)
                continue;

            if (original != null && original.Controller != null)
                original.Controller.DeregisterChild(view.Grid);

            UnityEngine.Object.Destroy(view.Grid.gameObject);
        }
    }
}
