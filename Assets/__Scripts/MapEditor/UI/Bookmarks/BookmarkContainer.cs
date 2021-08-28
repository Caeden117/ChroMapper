using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BookmarkContainer : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    private BookmarkManager manager;
    public BeatmapBookmark Data { get; private set; }


    public void Init(BookmarkManager manager, BeatmapBookmark data)
    {
        if (this.data != null) return;
        Data = data;
        this.manager = manager;
        GetComponent<Image>().color = data.Color;

        UpdateUI();
    }

    private void UpdateUI()
    {
        string name = data.Name.StripTMPTags();
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
        {
            name = $"<i>(This Bookmark has no name)</i>";
        }
        GetComponent<Tooltip>().tooltipOverride = name;
        GetComponent<Image>().color = data.Color;
    }

    // This fixes position of bookmarks to match aspect ratios
    public void RefreshPosition(float width)
    {
        float unitsPerBeat = width / manager.atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.loadedSong.length);
        RectTransform rectTransform = (RectTransform)transform;
        rectTransform.anchoredPosition = new Vector2(unitsPerBeat * data._time, 50);
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
                if (manager.shiftContext.started) PersistentUI.Instance.ShowColorInputBox("Mapper", "bookmark.update.color", HandleNewBookmarkColor, GetComponent<Image>().color);
                else PersistentUI.Instance.ShowInputBox("Mapper", "bookmark.update.dialog", HandleNewBookmarkName, null, data._name);
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

    public void Init(BookmarkManager manager, BeatmapBookmark data)
    {
        if (Data != null) return;
        Data = data;
        this.manager = manager;
        GetComponent<Image>().color = Random.ColorHSV(0, 1, 0.75f, 0.75f, 1, 1);

        UpdateUI();
    }

    private void UpdateUI()
    {
        var name = Data.Name.StripTMPTags();
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name)) name = "<i>(This Bookmark has no name)</i>";
        GetComponent<Tooltip>().TooltipOverride = name;
    }

    // This fixes position of bookmarks to match aspect ratios
    public void RefreshPosition(float width)
    {
        var unitsPerBeat = width / manager.Atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.LoadedSong.length);
        var rectTransform = (RectTransform)transform;
        rectTransform.anchoredPosition = new Vector2(unitsPerBeat * Data.Time, 50);
    }

    private void HandleNewBookmarkName(string res)
    {
        if (string.IsNullOrEmpty(res) || string.IsNullOrWhiteSpace(res)) return;

        Data.Name = res;
        UpdateUI();
    }

    private void HandleNewBookmarkColor(Color? res)
    {
        if (res == null) return;
        data._color = (Color)res;
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
