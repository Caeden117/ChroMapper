using UnityEngine;
using UnityEngine.UI;

public class NotePlacementUI : MonoBehaviour
{
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private BombPlacement bombPlacement;
    [SerializeField] private ObstaclePlacement obstaclePlacement;
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;

    [SerializeField] private Toggle[] chromaToggles;
    [SerializeField] private Toggle[] singleSaberDisabledToggles;
    public static bool delete; // boolean for delete tool

    private void Start()
    {
        BeatSaberSong.DifficultyBeatmapSet set = BeatSaberSongContainer.Instance.difficultyData.parentBeatmapSet;

        //ChromaToggle notes will be disabled until the mod is revived with some pretty breaking changes I have in mind.
        //The biggest of that is to create a new ChromaToggle characteristic that'll hold maps made for CT.
        foreach (Toggle toggle in chromaToggles)
        {
            if (set.beatmapCharacteristicName != "ChromaToggle")
            {
                toggle.interactable = false;
                Tooltip tooltip = toggle.GetComponent<Tooltip>();
                if (tooltip != null) tooltip.tooltip = "ChromaToggle coming soon!";
            }
        }

        foreach (Toggle toggle in singleSaberDisabledToggles)
        {
            if (set.beatmapCharacteristicName == "OneSaber")
            {
                toggle.interactable = false;
                Tooltip tooltip = toggle.GetComponent<Tooltip>();
                if (tooltip != null) tooltip.tooltip = "Single Saber only allows the right saber!";
            }
        }
    }

    public void RedNote(bool active)
    {
        if (active) UpdateValue(BeatmapNote.NOTE_TYPE_A);
    }

    public void BlueNote(bool active)
    {
        if (active) UpdateValue(BeatmapNote.NOTE_TYPE_B);
    }

    public void Bomb(bool active)
    {
        if (!active) return;
        notePlacement.IsActive = false;
        bombPlacement.IsActive = true;
        obstaclePlacement.IsActive = false;
        delete = false;
    }

    public void Wall(bool active)
    {
        if (!active) return;
        notePlacement.IsActive = false;
        bombPlacement.IsActive = false;
        obstaclePlacement.IsActive = true;
        delete = false;
    }

    public void RedAlt(bool active)
    {
        if (active) UpdateValue(BeatmapNote.NOTE_TYPE_A, true, BeatmapChromaNote.ALTERNATE);
    }

    public void BlueAlt(bool active)
    {
        if (active) UpdateValue(BeatmapNote.NOTE_TYPE_B, true, BeatmapChromaNote.ALTERNATE);
    }

    public void Mono(bool active)
    {
        if (active) UpdateValue(BeatmapNote.NOTE_TYPE_A, true, BeatmapChromaNote.MONOCHROME);
    }

    public void Duo(bool active)
    {
        if (active) UpdateValue(BeatmapNote.NOTE_TYPE_A, true, BeatmapChromaNote.DUOCHROME);
    }

    public void Delete(bool active)
    {
        if (!active) return;
        notePlacement.IsActive = false;
        bombPlacement.IsActive = false;
        obstaclePlacement.IsActive = false;
        delete = true;
    }

    private void UpdateValue(int v, bool isChroma = false, int chromaType = 0)
    {
        //if (notePlacement.atsc.IsPlaying) return;
        //if (!customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return; // not sure what these do?
        notePlacement.IsActive = true;
        bombPlacement.IsActive = false;
        obstaclePlacement.IsActive = false;
        delete = false;
        notePlacement.ChangeChromaToggle(isChroma);
        notePlacement.UpdateType(v);
        if (isChroma) notePlacement.UpdateChromaValue(chromaType);
    }
}
