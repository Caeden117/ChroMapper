using System.Collections;
using Beatmap.Base;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RefreshMapController : MonoBehaviour, CMInput.IRefreshMapActions
{
    [SerializeField] private MapLoader loader;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private TMP_FontAsset cancelFontAsset;
    [SerializeField] private TMP_FontAsset moreOptionsFontAsset;
    [SerializeField] private TMP_FontAsset thingYouCanRefreshFontAsset;
    private BeatSaberSong.DifficultyBeatmap diff;
    private BaseDifficulty map;
    private BeatSaberSong song;

    private void Start()
    {
        song = BeatSaberSongContainer.Instance.Song;
        diff = BeatSaberSongContainer.Instance.DifficultyData;
        map = BeatSaberSongContainer.Instance.Map;
    }

    public void OnRefreshMap(InputAction.CallbackContext context)
    {
        if (context.performed)
            InitiateRefreshConversation();
    }

    public void InitiateRefreshConversation() =>
        PersistentUI.Instance.ShowDialogBox("Mapper", "refreshmap",
            HandleFirstLayerConversation,
            new[]
            {
                "refreshmap.notes", "refreshmap.walls", "refreshmap.events", "refreshmap.other", "refreshmap.full",
                "refreshmap.cancel"
            },
            new[]
            {
                thingYouCanRefreshFontAsset, thingYouCanRefreshFontAsset, thingYouCanRefreshFontAsset,
                thingYouCanRefreshFontAsset, thingYouCanRefreshFontAsset, cancelFontAsset
            });

    private void HandleFirstLayerConversation(int res)
    {
        switch (res)
        {
            case 0:
                RefreshMap(true, false, false, false, false);
                break;
            case 1:
                RefreshMap(false, true, false, false, false);
                break;
            case 2:
                RefreshMap(false, false, true, false, false);
                break;
            case 3:
                RefreshMap(false, false, false, true, false);
                break;
            case 4:
                RefreshMap(false, false, false, false, true);
                break;
        }
    }

    private void RefreshMap(bool notes, bool obstacles, bool events, bool others, bool full)
    {
        map = song.GetMapFromDifficultyBeatmap(diff);
        loader.UpdateMapData(map);
        
        loader.LoadObjects(map.BpmChanges);
        
        var currentSongBpmTime = atsc.CurrentSongBpmTime;
        atsc.MoveToSongBpmTime(0);

        if (notes || full) loader.LoadObjects(map.Notes);
        
        if (obstacles || full) loader.LoadObjects(map.Obstacles);
        
        if (events || full) loader.LoadObjects(map.Events);
        
        if (others || full) loader.LoadObjects(map.CustomEvents);
        
        if ((notes || full) && Settings.Instance.Load_MapV3)
        {
            loader.LoadObjects(map.Arcs);
            loader.LoadObjects(map.Chains);
        }
        
        if (full) BeatSaberSongContainer.Instance.Map.MainNode = map.MainNode;
        
        tracksManager.RefreshTracks();
        atsc.MoveToSongBpmTime(currentSongBpmTime);
    }
}
