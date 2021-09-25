using SimpleJSON;
using UnityEngine;

public abstract class TrackLaneRingsManagerBase : MonoBehaviour
{
    public abstract void HandlePositionEvent(JSONNode customData = null);

    public abstract void HandleRotationEvent(JSONNode customData = null);

    public abstract Object[] GetToDestroy();
}
