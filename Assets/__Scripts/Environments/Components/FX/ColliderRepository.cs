using System.Collections.Generic;
using UnityEngine;

public class ColliderRepository : MonoBehaviour
{
    public readonly Dictionary<Collider, ColliderFx> Colliders = new();
    public void Register(ColliderFx fx) => Colliders.TryAdd(fx.Collider, fx);
    public bool TryGet(Collider coll, out ColliderFx fx) => Colliders.TryGetValue(coll, out fx);
}
