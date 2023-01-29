using Beatmap.Base;
using UnityEngine;

public abstract class TrackLaneRingsManagerBase : MonoBehaviour
{
    public abstract void HandlePositionEvent(BaseEvent evt);

    public abstract void HandleRotationEvent(BaseEvent evt);

    public abstract Object[] GetToDestroy();
}
