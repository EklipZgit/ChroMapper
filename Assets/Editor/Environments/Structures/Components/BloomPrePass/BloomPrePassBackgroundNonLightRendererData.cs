using UnityEngine;

public class
    BloomPrePassBackgroundNonLightRendererData : EnvironmentComponentData<BloomPrePassBackgroundNonLightRenderer>
{
    public int ExecutionTimeType;
    public bool KeepDefaultRendering;
    public bool UseCustomMaterial;
    public string CustomMaterial;
    public bool UseCustomPropertyBlock;
    public string RendererId;
    public string MeshFilterId;

    public override void SearchAndFillComponents(
        GameObject self,
        BloomPrePassBackgroundNonLightRenderer comp,
        CreateContainer container)
    {
        comp.CustomMaterial = container.Library.Materials.Lookup[CustomMaterial];
        comp.Renderer = container.GetGameObjectOrNull(RendererId, self).GetComponent<Renderer>();
        comp.MeshFilter = container.GetGameObjectOrNull(MeshFilterId, self).GetComponent<MeshFilter>();
    }

    public override void CopyTo(BloomPrePassBackgroundNonLightRenderer comp)
    {
        comp.ExecutionTimeType = (BloomPrePassNonLightPass.ExecutionTime)ExecutionTimeType;
        comp.KeepDefaultRendering = KeepDefaultRendering;
        comp.UseCustomMaterial = UseCustomMaterial;
        comp.UseCustomPropertyBlock = UseCustomPropertyBlock;
    }
}
