using UnityEngine;

public abstract class PlatformEventHandler : MonoBehaviour
{
    public abstract int[] ListeningEventTypes { get; }

    public abstract void OnEventTrigger(int type, MapEvent @event);
}
