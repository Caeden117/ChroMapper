using UnityEngine;

public class MultiClientSubscribeBroadcaster : MonoBehaviour
{
    private void Start()
    {
        var client = BeatSaberSongContainer.Instance.MultiMapperConnection;

        client?.SubscribeToCollectionEvents();
    }

    private void OnDestroy()
    {
        var client = BeatSaberSongContainer.Instance.MultiMapperConnection;

        client?.UnsubscribeFromCollectionEvents();
    }
}
