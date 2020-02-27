using System.Collections;
using UnityEngine;
using TMPro;

public class RefreshMapController : MonoBehaviour
{
    [SerializeField] private MapLoader loader;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private TMP_FontAsset cancelFontAsset;
    [SerializeField] private TMP_FontAsset moreOptionsFontAsset;
    [SerializeField] private TMP_FontAsset thingYouCanRefreshFontAsset;

    BeatSaberSong song;
    BeatSaberSong.DifficultyBeatmap diff;
    BeatSaberMap map;

    private void Start()
    {
        song = BeatSaberSongContainer.Instance.song;
        diff = BeatSaberSongContainer.Instance.difficultyData;
        map = BeatSaberSongContainer.Instance.map;
    }

    public void InitiateRefreshConversation()
    {
        PersistentUI.Instance.ShowDialogBox("So, you'd like to refresh some parts of the map?\n\n" +
            "Please click on the options below to refresh parts of the map you want to refresh.",
            HandleFirstLayerConversation, "Notes", "Obstacles", ">>",
            thingYouCanRefreshFontAsset, thingYouCanRefreshFontAsset, moreOptionsFontAsset);
    }

    private void HandleFirstLayerConversation(int res)
    {
        switch (res)
        {
            case 0:
                StartCoroutine(RefreshMap(true, false, false, false, false));
                break;
            case 1:
                StartCoroutine(RefreshMap(false, true, false, false, false));
                break;
            case 2:
                PersistentUI.Instance.ShowDialogBox("So, you'd like to refresh some parts of the map?\n\n" +
                    "Please click on the options below to refresh parts of the map you want to refresh.",
                    HandleSecondLayerConversation, "Events", "Others", ">>",
                    thingYouCanRefreshFontAsset, thingYouCanRefreshFontAsset, moreOptionsFontAsset);
                break;
        }
    }

    private void HandleSecondLayerConversation(int res)
    {
        switch (res)
        {
            case 0:
                StartCoroutine(RefreshMap(false, false, true, false, false));
                break;
            case 1:
                StartCoroutine(RefreshMap(false, false, false, true, false));
                break;
            case 2:
                PersistentUI.Instance.ShowDialogBox("So, you'd like to refresh some parts of the map?\n\n" +
                    "Please click on the options below to refresh parts of the map you want to refresh.",
                    HandleThirdLayerConversation, "Full Refresh", "Cancel", "<<",
                    thingYouCanRefreshFontAsset, cancelFontAsset, moreOptionsFontAsset);
                break;
        }
    }
    private void HandleThirdLayerConversation(int res)
    {
        switch (res)
        {
            case 0:
                StartCoroutine(RefreshMap(false, false, false, false, true));
                break;
            case 2:
                PersistentUI.Instance.ShowDialogBox("So, you'd like to refresh some parts of the map?\n\n" +
                    "Please click on the options below to refresh parts of the map you want to refresh.",
                    HandleFirstLayerConversation, "Notes", "Obstacles", ">>",
                    thingYouCanRefreshFontAsset, thingYouCanRefreshFontAsset, moreOptionsFontAsset);
                break;
        }
    }

    private IEnumerator RefreshMap(bool notes, bool obstacles, bool events, bool others, bool full)
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        map = song.GetMapFromDifficultyBeatmap(diff);
        loader.UpdateMapData(map);
        float currentBeat = atsc.CurrentBeat;
        atsc.MoveToTimeInBeats(0);
        if (notes || full) yield return StartCoroutine(loader.LoadObjects(map._notes));
        if (obstacles || full) yield return StartCoroutine(loader.LoadObjects(map._obstacles));
        if (events || full) yield return StartCoroutine(loader.LoadObjects(map._events));
        if (others || full) yield return StartCoroutine(loader.LoadObjects(map._BPMChanges));
        if (others || full) yield return StartCoroutine(loader.LoadObjects(map._customEvents));
        tracksManager.RefreshTracks();
        SelectionController.RefreshMap();
        atsc.MoveToTimeInBeats(currentBeat);
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }

}
