using UnityEngine;
using UnityEngine.Localization;

public class ChangeSortTooltipOnSortChange : MonoBehaviour
{
    [SerializeField] private SongList songList;
    [SerializeField] private Tooltip targetTooltip;

    [Space] [SerializeField] private LocalizedString sortByNameString;

    [SerializeField] private LocalizedString sortByArtistString;
    [SerializeField] private LocalizedString sortByModifiedString;

    private void Start() => songList.SortTypeChanged += SongList_OnSortTypeChanged;

    private void OnDestroy() => songList.SortTypeChanged -= SongList_OnSortTypeChanged;

    private void SongList_OnSortTypeChanged(SongList.SongSortType sortType)
    {
        switch (sortType)
        {
            case SongList.SongSortType.Name:
                targetTooltip.LocalizedTooltip = sortByNameString;
                break;
            case SongList.SongSortType.Artist:
                targetTooltip.LocalizedTooltip = sortByArtistString;
                break;
            case SongList.SongSortType.Modified:
                targetTooltip.LocalizedTooltip = sortByModifiedString;
                break;
        }

        // Hacky workaround but whatever
        if (targetTooltip.TooltipActive)
        {
            targetTooltip.OnPointerExit(null);
            targetTooltip.OnPointerEnter(null);
        }
    }
}
