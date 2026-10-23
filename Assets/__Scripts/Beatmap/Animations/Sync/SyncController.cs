using UnityEngine;

public abstract class SyncController : MonoBehaviour, ISync
{
    public AudioTimeSyncController Atsc;

    protected float StartTime { get; private set; }
    protected float SongTime { get; set; }

    public void SetStartTime(float time)
    {
        StartTime = time;
        SongTime = time;
    }

    public abstract void ResetTime();
    public abstract void Sync(float speed);

    private void Update()
    {
        var deltaTime = Time.deltaTime;
        var songTime = Atsc.CurrentSeconds;
        var deltaSongTime = songTime - SongTime;
        SongTime = songTime;

        if (deltaTime > 0 && deltaSongTime > 0)
            Sync(deltaSongTime / deltaTime);
        else if (!Atsc.IsPlaying && deltaSongTime < 0)
            ResetTime();
        else
            Sync(0);
    }
}
