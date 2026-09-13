using UnityEngine;

public class GridElementControllerData : EnvironmentComponentData<GridElementController>
{
    public int GridPivotAnchor;
    public int MaterialPropertyBlockController;
    public int GridElementRenderer;
    public string GridPivotPropertyName;
    public string GridElementIndexPropertyName;
    public Vector3 IDVector;

    public override void FillComponents(GameObject self, GridElementController comp, CreateContainer container)
    {
        comp.GridPivotAnchor = container.GetComponentOrNull<Transform>(GridPivotAnchor);
        comp.MaterialPropertyBlockController =
            container.GetComponentOrNull<MaterialPropertyBlockController>(MaterialPropertyBlockController);
        comp.GridElementRenderer = container.GetComponentOrNull<MeshRenderer>(GridElementRenderer);
        comp.GridPivotPropertyName = GridPivotPropertyName;
        comp.GridElementIndexPropertyName = GridElementIndexPropertyName;
        comp.IDVector = IDVector;
    }
}
