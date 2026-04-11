using UnityEngine;

public class GridElementControllerData : EnvironmentComponentData<GridElementController>
{
    public string GridPivotAnchor;
    public string MaterialPropertyBlockController;
    public string GridElementRenderer;
    public string GridPivotPropertyName;
    public string GridElementIndexPropertyName;
    public Vector3 IDVector;

    public override void SearchAndFillComponents(GameObject self, GridElementController comp, CreateContainer container)
    {
        comp.GridPivotAnchor = container.GetGameObjectOrNull(GridPivotAnchor, self).GetComponent<Transform>();
        comp.MaterialPropertyBlockController =
            container
                .GetGameObjectOrNull(MaterialPropertyBlockController, self)
                .GetComponent<MaterialPropertyBlockController>();
        comp.GridElementRenderer = container.GetGameObjectOrNull(GridElementRenderer, self).GetComponent<MeshRenderer>();
    }

    public override void CopyTo(GridElementController comp)
    {
        comp.GridPivotPropertyName = GridPivotPropertyName;
        comp.GridElementIndexPropertyName = GridElementIndexPropertyName;
        comp.IDVector = IDVector;
    }
}
