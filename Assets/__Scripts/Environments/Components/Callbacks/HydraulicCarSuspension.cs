using System.Collections.Generic;
using UnityEngine;

public class HydraulicCarSuspension : MonoBehaviour
{
    [SerializeField] public GenericCallbackEventEffect ContractEffect;
    [SerializeField] public GenericCallbackEventEffect ExpandEffect;
    [SerializeField] public int[] EventValues;

    [Space] [SerializeField] public ConfigurableJoint Joint;
    [SerializeField] public float ContractDistance;
    [SerializeField] public float ExpandDistance;

    [Space] [SerializeField] public Rigidbody Rigidbody;

    private HashSet<int> eventValuesHashSet;

    protected void Awake()
    {
        eventValuesHashSet = new HashSet<int>(EventValues);
    }

    private void Start()
    {
        var p = ContractEffect.GetCurrentState();
        if (p.index != -1) HandleContractStateChanged(p);
        p = ExpandEffect.GetCurrentState();
        if (p.index != -1) HandleExpandStateChanged(p);
    }

    private void OnEnable() => TrySubscribe();
    protected void OnDisable() => TryUnsubscribe();
    protected void OnDestroy() => TryUnsubscribe();

    private void TrySubscribe()
    {
        if (ContractEffect != null) ContractEffect.OnStateChanged += HandleContractStateChanged;
        if (ExpandEffect != null) ExpandEffect.OnStateChanged += HandleExpandStateChanged;
    }

    private void TryUnsubscribe()
    {
        if (ContractEffect != null) ContractEffect.OnStateChanged -= HandleContractStateChanged;
        if (ExpandEffect != null) ExpandEffect.OnStateChanged -= HandleExpandStateChanged;
    }

    private void HandleContractStateChanged((int index, BasicEventStateData state) data)
    {
        if (!eventValuesHashSet.Contains(data.state.Base.Value)) return;
        Joint.targetPosition = Vector3.up * ContractDistance;
        Rigidbody.WakeUp();
    }

    private void HandleExpandStateChanged((int index, BasicEventStateData state) data)
    {
        if (!eventValuesHashSet.Contains(data.state.Base.Value)) return;
        Joint.targetPosition = Vector3.up * ExpandDistance;
        Rigidbody.WakeUp();
    }
}
