using UnityEngine;
using UnityEngine.Localization;

public class ChangeSortTooltipOnSortChange : MonoBehaviour
{
    [SerializeField] private SongList songList;
    [SerializeField] private Tooltip targetTooltip;
    [Space]
    [SerializeField] private LocalizedString sortByNameString;
    [SerializeField] private LocalizedString sortByArtistString;
    [SerializeField] private LocalizedString sortByModifiedString;

    private void Start()
    {
        songList.OnSortTypeChanged += SongList_OnSortTypeChanged;
    }

    private void SongList_OnSortTypeChanged(SongList.SongSortType sortType)
    {
        switch (sortType)
        {
            case SongList.SongSortType.Name:
                targetTooltip.tooltip = sortByNameString;
                break;
            case SongList.SongSortType.Artist:
                targetTooltip.tooltip = sortByArtistString;
                break;
            case SongList.SongSortType.Modified:
                targetTooltip.tooltip = sortByModifiedString;
                break;
        }

        // Hacky workaround but whatever
        if (targetTooltip.TooltipActive)
        {
            targetTooltip.OnPointerExit(null);
            targetTooltip.OnPointerEnter(null);
        }
    }

    private void OnDestroy()
    {
        songList.OnSortTypeChanged -= SongList_OnSortTypeChanged;
    }
}
