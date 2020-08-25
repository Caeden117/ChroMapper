using UnityEngine;

public abstract class TrackLaneRingsManagerBase : MonoBehaviour
{
    abstract public void HandlePositionEvent();

    abstract public void HandleRotationEvent(SimpleJSON.JSONNode customData = null);

    abstract public Object[] GetToDestroy();
}
