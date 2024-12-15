using System.Collections;
using Beatmap.Base;
using Beatmap.Info;
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
    private InfoDifficulty infoDifficulty;
    private BaseDifficulty map;
    private BaseInfo info;

    private void Start()
    {
        info = BeatSaberSongContainer.Instance.Info;
        infoDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;
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
        map = BeatSaberSongUtils.GetMapFromInfoFiles(info, infoDifficulty);
        loader.UpdateMapData(map);
        
        var currentSongBpmTime = atsc.CurrentSongBpmTime;
        atsc.MoveToSongBpmTime(0);

        if (full)
        {
            BeatSaberSongContainer.Instance.Map = map;
            loader.HardRefresh();
            atsc.MoveToSongBpmTime(currentSongBpmTime);
            return;
        }

        BeatSaberSongContainer.Instance.Map.BpmEvents = map.BpmEvents;
        loader.LoadObjects(map.BpmEvents);

        if (notes)
        {
            BeatSaberSongContainer.Instance.Map.Notes = map.Notes;
            BeatSaberSongContainer.Instance.Map.Arcs = map.Arcs;
            BeatSaberSongContainer.Instance.Map.Chains = map.Chains;
            loader.LoadObjects(map.Notes);
            loader.LoadObjects(map.Arcs);
            loader.LoadObjects(map.Chains);
        }

        if (obstacles)
        {
            BeatSaberSongContainer.Instance.Map.Obstacles = map.Obstacles;
            loader.LoadObjects(map.Obstacles);
        }

        if (events)
        {
            BeatSaberSongContainer.Instance.Map.Events = map.Events;
            loader.LoadObjects(map.Events);
        }

        if (others)
        {
            BeatSaberSongContainer.Instance.Map.CustomEvents = map.CustomEvents;
            loader.LoadObjects(map.CustomEvents);
        }
        
        tracksManager.RefreshTracks();
        atsc.MoveToSongBpmTime(currentSongBpmTime);
    }
}
