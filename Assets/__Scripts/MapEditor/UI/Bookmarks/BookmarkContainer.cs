using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BookmarkContainer : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    private BookmarkManager manager;
    public BeatmapBookmark Data { get; private set; }


    public void Init(BookmarkManager manager, BeatmapBookmark data)
    {
        if (Data != null) return;
        Data = data;

        this.manager = manager;
        GetComponent<Image>().color = data.Color;

        UpdateUI();
    }

    private void UpdateUI()
    {
        var name = Data.Name.StripTMPTags();

        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
        {
            name = $"<i>(This Bookmark has no name)</i>";
        }

        GetComponent<Tooltip>().TooltipOverride = name;
        GetComponent<Image>().color = Data.Color;
    }

    // This fixes position of bookmarks to match aspect ratios
    public void RefreshPosition(float width)
    {
        var unitsPerBeat = width / manager.Atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.LoadedSong.length);
        var rectTransform = (RectTransform)transform;
        rectTransform.anchoredPosition = new Vector2(unitsPerBeat * Data.Time, 50);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Middle:
                PersistentUI.Instance.ShowDialogBox("Mapper", "bookmark.delete", HandleDeleteBookmark,
                    PersistentUI.DialogBoxPresetType.YesNo);
                break;
            case PointerEventData.InputButton.Right:
                if (manager.ShiftContext.started)
                {
                    PersistentUI.Instance.ShowColorInputBox("Mapper", "bookmark.update.color", HandleNewBookmarkColor, GetComponent<Image>().color);
                }
                else
                {
                    PersistentUI.Instance.ShowInputBox("Mapper", "bookmark.update.dialog", HandleNewBookmarkName, null, Data.Name);
                }

                break;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            manager.Tipc.PointerDown();
            manager.Atsc.MoveToTimeInBeats(Data.Time);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) manager.Tipc.PointerUp();
    }

    private void HandleNewBookmarkName(string res)
    {
        if (string.IsNullOrEmpty(res) || string.IsNullOrWhiteSpace(res)) return;

        Data.Name = res;
        manager.BookmarksUpdated.Invoke();
        UpdateUI();
    }

    private void HandleNewBookmarkColor(Color? res)
    {
        if (res == null) return;
        Data.Color = (Color)res;
        manager.BookmarksUpdated.Invoke();
        UpdateUI();
    }

    internal void HandleDeleteBookmark(int res)
    {
        if (res == 0)
        {
            manager.DeleteBookmark(this);
            Destroy(gameObject);
        }
    }
}
