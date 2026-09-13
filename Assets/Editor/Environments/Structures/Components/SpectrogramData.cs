using System.Linq;
using UnityEngine;

public class SpectrogramData : EnvironmentComponentData<Spectrogram>
{
    public bool SetAsGlobal;
    public int[] MeshRenderers;
    public int MaterialPropertyBlockController;

    public override void FillComponents(GameObject self, Spectrogram comp, CreateContainer container)
    {
        comp.SpectrogramDataProvider = container.Descriptor.SpectrogramDataProvider;
        comp.MeshRenderers = MeshRenderers.Select(container.GetComponentOrNull<MeshRenderer>).ToArray();
        comp.MpbController =
            container.GetComponentOrNull<MaterialPropertyBlockController>(MaterialPropertyBlockController);
        comp.SetAsGlobal = SetAsGlobal;
    }
}
