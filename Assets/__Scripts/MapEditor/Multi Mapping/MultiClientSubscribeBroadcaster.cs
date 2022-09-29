using UnityEngine;

public class MultiClientSubscribeBroadcaster : MonoBehaviour
{
    private void Start()
    {
        var client = BeatSaberSongContainer.Instance.MultiMapperConnection;

        client?.SubscribeToCollectionEvents();
        client?.UpdateCachedPoses();
    }

    private void OnDestroy()
    {
        var client = BeatSaberSongContainer.Instance.MultiMapperConnection;

        client?.UnsubscribeFromCollectionEvents();
        client?.Dispose();

        BeatSaberSongContainer.Instance.MultiMapperConnection = null;
    }
}
