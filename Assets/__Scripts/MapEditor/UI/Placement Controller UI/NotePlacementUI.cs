using UnityEngine;

public class NotePlacementUI : MonoBehaviour
{
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private BombPlacement bombPlacement;
    [SerializeField] private ObstaclePlacement obstaclePlacement;

    public void RedNote()
    {
        UpdateValue(BeatmapNote.NOTE_TYPE_A);
    }

    public void BlueNote()
    {
        UpdateValue(BeatmapNote.NOTE_TYPE_B);
    }

    public void Bomb()
    {
        notePlacement.IsActive = false;
        bombPlacement.IsActive = true;
        obstaclePlacement.IsActive = false;
    }

    public void Wall()
    {
        notePlacement.IsActive = false;
        bombPlacement.IsActive = false;
        obstaclePlacement.IsActive = true;
    }

    public void RedAlt()
    {
        UpdateValue(BeatmapNote.NOTE_TYPE_A, true, BeatmapChromaNote.ALTERNATE);
    }

    public void BlueAlt()
    {
        UpdateValue(BeatmapNote.NOTE_TYPE_B, true, BeatmapChromaNote.ALTERNATE);
    }

    public void Mono()
    {
        UpdateValue(BeatmapNote.NOTE_TYPE_A, true, BeatmapChromaNote.MONOCHROME);
    }

    public void Duo()
    {
        UpdateValue(BeatmapNote.NOTE_TYPE_A, true, BeatmapChromaNote.DUOCHROME);
    }

    private void UpdateValue(int v, bool isChroma = false, int chromaType = 0)
    {
        if (notePlacement.atsc.IsPlaying) return;
        notePlacement.IsActive = true;
        bombPlacement.IsActive = false;
        obstaclePlacement.IsActive = false;
        notePlacement.ChangeChromaToggle(isChroma);
        notePlacement.UpdateType(v);
        if (isChroma) notePlacement.UpdateChromaValue(chromaType);
    }
}
