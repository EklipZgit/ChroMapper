using System.Linq;
using UnityEngine;

public class SpectrogramData : EnvironmentComponentData<Spectrogram>
{
    public bool SetAsGlobal;
    public string[] MeshRenderers;
    public string MaterialPropertyBlockController;

    public override void SearchAndFillComponents(GameObject self, Spectrogram comp, CreateContainer container)
    {
        comp.MeshRenderers =
            MeshRenderers
                .Select(o => container.GetGameObjectOrNull(o, self).GetComponent<MeshRenderer>())
                .ToArray();
        comp.MpbController = container
            .GetGameObjectOrNull(MaterialPropertyBlockController, self)
            .GetComponent<MaterialPropertyBlockController>();
    }

    public override void CopyTo(Spectrogram comp) => comp.SetAsGlobal = SetAsGlobal;
}
