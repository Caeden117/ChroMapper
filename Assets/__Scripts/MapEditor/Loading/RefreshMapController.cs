using System.Collections;
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
    private BeatSaberMap map;
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
                StartCoroutine(RefreshMap(true, false, false, false, false));
                break;
            case 1:
                StartCoroutine(RefreshMap(false, true, false, false, false));
                break;
            case 2:
                StartCoroutine(RefreshMap(false, false, true, false, false));
                break;
            case 3:
                StartCoroutine(RefreshMap(false, false, false, true, false));
                break;
            case 4:
                StartCoroutine(RefreshMap(false, false, false, false, true));
                break;
        }
    }

    private IEnumerator RefreshMap(bool notes, bool obstacles, bool events, bool others, bool full)
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        map = song.GetMapFromDifficultyBeatmap(diff);
        loader.UpdateMapData(map);
        var currentBeat = atsc.CurrentBeat;
        atsc.MoveToTimeInBeats(0);
        if (notes || full) yield return StartCoroutine(loader.LoadObjects(map.Notes));
        if (obstacles || full) yield return StartCoroutine(loader.LoadObjects(map.Obstacles));
        if (events || full) yield return StartCoroutine(loader.LoadObjects(map.Events));
        if (others || full) yield return StartCoroutine(loader.LoadObjects(map.BpmChanges));
        if (others || full) yield return StartCoroutine(loader.LoadObjects(map.CustomEvents));
        if (full) BeatSaberSongContainer.Instance.Map.MainNode = map.MainNode;
        tracksManager.RefreshTracks();
        SelectionController.RefreshMap();
        atsc.MoveToTimeInBeats(currentBeat);
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }
}
