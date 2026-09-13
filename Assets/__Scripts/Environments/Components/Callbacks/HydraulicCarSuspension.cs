using System.Collections.Generic;
using UnityEngine;

public class HydraulicCarSuspension : MonoBehaviour
{
    [SerializeField] public GenericCallbackEventEffect ContractEffect;
    [SerializeField] public int[] ContractEventValues;
    [SerializeField] public GenericCallbackEventEffect ExpandEffect;
    [SerializeField] public int[] ExpandEventValues;

    [Space] [SerializeField] public SpringJoint SpringJoint;
    [SerializeField] public float ContractDistance = 0.3f;
    [SerializeField] public float ExpandDistance = 0.4f;

    [Space] [SerializeField] public Rigidbody Rigidbody;

    private HashSet<int> contractEventValuesHashSet;
    private HashSet<int> expandEventValuesHashSet;

    protected void Awake()
    {
        contractEventValuesHashSet = new HashSet<int>(ContractEventValues);
        expandEventValuesHashSet = new HashSet<int>(ExpandEventValues);
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
        if (!contractEventValuesHashSet.Contains(data.state.Base.Value)) return;
        SpringJoint.minDistance = ContractDistance;
        SpringJoint.maxDistance = ContractDistance;
        Rigidbody.WakeUp();
    }

    private void HandleExpandStateChanged((int index, BasicEventStateData state) data)
    {
        if (!expandEventValuesHashSet.Contains(data.state.Base.Value)) return;
        SpringJoint.minDistance = ExpandDistance;
        SpringJoint.maxDistance = ExpandDistance;
        Rigidbody.WakeUp();
    }
}
