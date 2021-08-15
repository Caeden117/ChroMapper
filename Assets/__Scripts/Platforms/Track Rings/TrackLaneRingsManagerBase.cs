using UnityEngine;

public abstract class TrackLaneRingsManagerBase : MonoBehaviour
{
    abstract public void HandlePositionEvent(SimpleJSON.JSONNode customData = null);

    abstract public void HandleRotationEvent(SimpleJSON.JSONNode customData = null);

    abstract public Object[] GetToDestroy();
}
