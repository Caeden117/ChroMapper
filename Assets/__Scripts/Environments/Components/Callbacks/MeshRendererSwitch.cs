using UnityEngine;

public class MeshRendererSwitch : MonoBehaviour
{
    public GenericCallbackEventEffect Effect;

    public Renderer[] NormalRenderers;
    public Renderer[] BoostRenderers;

    private void Start()
    {
        Effect.OnStateChanged += HandleStateChanged;
        var p = Effect.GetCurrentState();
        if (p.index != -1) HandleStateChanged(p);
    }

    private void OnDestroy() => Effect.OnStateChanged -= HandleStateChanged;

    private void HandleStateChanged((int index, BasicEventStateData state) data)
    {
        var value = data.state.Base.Value == 1;
        for (var i = 0; i < NormalRenderers.Length; i++) NormalRenderers[i].enabled = !value;
        for (var i = 0; i < BoostRenderers.Length; i++) BoostRenderers[i].enabled = value;
    }
}
