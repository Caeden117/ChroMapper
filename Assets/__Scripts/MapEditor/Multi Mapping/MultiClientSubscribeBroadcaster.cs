using UnityEngine;

public class MultiClientSubscribeBroadcaster : MonoBehaviour
{
    private void Start()
    {
        var client = BeatSaberSongContainer.Instance.MultiMapperConnection;

        client?.SubscribeToCollectionEvents();
        client?.UpdateCachedPoses();

        LoadInitialMap.LevelLoadedEvent += LevelLoadedEvent;
    }

    private void LevelLoadedEvent() => ActionCachingPacketHandler.FlushCache();

    private void OnDestroy()
    {
        var client = BeatSaberSongContainer.Instance.MultiMapperConnection;

        client?.UnsubscribeFromCollectionEvents();
        client?.Dispose();

        BeatSaberSongContainer.Instance.MultiMapperConnection = null;

        LoadInitialMap.LevelLoadedEvent -= LevelLoadedEvent;
    }
}
