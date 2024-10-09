using System;
using Beatmap.Base.Customs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class BookmarkContainer : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    private BookmarkManager manager;
    public BaseBookmark Data { get; private set; }

    public void Init(BookmarkManager manager, BaseBookmark data)
    {
        if (Data != null) return;
        Data = data;

        this.manager = manager;

        UpdateUI();
    }

    public void UpdateUI()
    {
        UpdateUIText();
        UpdateUIColor();
        UpdateUIWidth();
    }

    public void UpdateUIText()
    {
        var text = Data.Name.StripTMPTags();

        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
        {
            text = $"<i>(This Bookmark has no name)</i>";
        }

        if (Settings.Instance.BookmarkTooltipTimeInfo)
        {
            var span = TimeSpan.FromSeconds(manager.Atsc.GetSecondsFromBeat(Data.SongBpmTime));
            text += $" [{Math.Round(Data.JsonTime, 2)} | {span:mm':'ss}]";
        }

        GetComponent<Tooltip>().TooltipOverride = text;
    }

    public void UpdateUIColor() {
        var brightenedColor = Data.Color
            .Multiply(Settings.Instance.BookmarkTimelineBrightness)
            .WithAlpha(Data.Color.a);
        GetComponent<Image>().color = brightenedColor;
    }

    public void UpdateUIWidth()
    {
        var bookRect = this.transform as RectTransform;
        bookRect.sizeDelta = new Vector2(Settings.Instance.BookmarkTimelineWidth, bookRect.sizeDelta.y);
    }

    // This fixes position of bookmarks to match aspect ratios
    public void RefreshPosition(float width)
    {
        var unitsPerBeat = width / manager.Atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.LoadedSong.length);
        var rectTransform = (RectTransform)transform;
        rectTransform.anchoredPosition = new Vector2(unitsPerBeat * Data.SongBpmTime, 50);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (eventData.button)
        {
            case PointerEventData.InputButton.Left:
                break;
            case PointerEventData.InputButton.Middle:
                PersistentUI.Instance.ShowDialogBox("Mapper", "bookmark.delete", HandleDeleteBookmark,
                    PersistentUI.DialogBoxPresetType.YesNo);
                break;
            case PointerEventData.InputButton.Right:
                DisplayBookmarkEditUI();
                break;
            default:
                return;
        }
    }

    private void DisplayBookmarkEditUI()
    {
        var title = LocalizationSettings.StringDatabase.GetLocalizedString("Mapper", "bookmark.update.dialog");
        var dialogBox = PersistentUI.Instance.CreateNewDialogBox().WithTitle(title);

        var textBox = dialogBox.AddComponent<TextBoxComponent>()
            .WithInitialValue(Data.Name)
            .WithLabel("Mapper", "bookmark.dialog.name");

        var colorPicker = dialogBox
            .AddComponent<NestedColorPickerComponent>()
            .WithInitialValue(Data.Color)
            .WithLabel("Mapper", "bookmark.dialog.color");

        Action handleEdit = () =>
        {
            if (!string.IsNullOrWhiteSpace(textBox.Value))
            {
                Data.Name = textBox.Value;
            }
            if (colorPicker.Value != null)
            {
                Data.Color = colorPicker.Value;
            }
            manager.BookmarksUpdated.Invoke();
            UpdateUIText();
            UpdateUIColor();
        };

        var cancelButton = dialogBox
            .AddFooterButton(() => { },
                LocalizationSettings.StringDatabase.GetLocalizedString(nameof(PersistentUI), "cancel"));

        var submitButton = dialogBox
            .AddFooterButton(handleEdit,
                LocalizationSettings.StringDatabase.GetLocalizedString(nameof(PersistentUI), "submit"));

        dialogBox.OnQuickSubmit(handleEdit);
        dialogBox.Open();
    }

    #region Timeline Playback
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            manager.Tipc.PointerDown();
            manager.Atsc.MoveToJsonTime(Data.JsonTime);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            manager.Tipc.PointerUp();
        }
    }
    #endregion

    internal void HandleDeleteBookmark(int res)
    {
        if (res == 0)
        {
            manager.DeleteBookmark(this);
            Destroy(gameObject);
        }
    }
}
