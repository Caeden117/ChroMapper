using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// TODO: Refactor all this Bookmark bullshit to fit every other object in ChroMapper (using BeatmapObjectContainerCollection, BeatmapObjectContainer, etc. etc.)
public class BookmarkManager : MonoBehaviour, CMInput.IBookmarksActions
{
    // -10 twice for the distance from screen edges, -5 for half the width of one bookmark
    public const float CanvasWidthOffset = -20f;

    private static readonly System.Random rng = new System.Random();

    [SerializeField] private GameObject bookmarkContainerPrefab;
    [FormerlySerializedAs("atsc")] public AudioTimeSyncController Atsc;
    [FormerlySerializedAs("tipc")] public TimelineInputPlaybackController Tipc;
    [SerializeField] private RectTransform timelineCanvas;

    public InputAction.CallbackContext ShiftContext;

    internal List<BookmarkContainer> bookmarkContainers = new List<BookmarkContainer>();

    public Action BookmarksUpdated;
    public event Action<BaseObject> BookmarkAdded;
    public event Action<BaseObject> BookmarkDeleted;

    private float previousCanvasWidth;

    private DialogBox createBookmarkDialogBox;
    private TextBoxComponent bookmarkName;
    private NestedColorPickerComponent bookmarkColor;

    private IEnumerator Start()
    {
        // Create and cache dialog box for later use
        createBookmarkDialogBox = PersistentUI.Instance
            .CreateNewDialogBox()
            .WithTitle("Mapper", "bookmark.dialog")
            .DontDestroyOnClose();

        bookmarkName = createBookmarkDialogBox
            .AddComponent<TextBoxComponent>()
            .WithLabel("Mapper", "bookmark.dialog.name")
            .WithInitialValue("Mapper", "bookmark.dialog.default");

        bookmarkColor = createBookmarkDialogBox
            .AddComponent<NestedColorPickerComponent>()
            .WithLabel("Mapper", "bookmark.dialog.color")
            .WithAlpha();

        // Enable quick submit
        createBookmarkDialogBox.OnQuickSubmit(() => CreateNewBookmark(bookmarkName.Value, bookmarkColor.Value));

        // Cancel button
        createBookmarkDialogBox.AddFooterButton(null, "PersistentUI", "cancel");

        // Submit/OK button
        createBookmarkDialogBox.AddFooterButton(
            () => CreateNewBookmark(bookmarkName.Value, bookmarkColor.Value), "PersistentUI", "ok");

        // Wait for time
        yield return new WaitForSeconds(0.1f);

        ConvertBookmarkTimesFromOldDevVersions();

        bookmarkContainers = InstantiateBookmarkContainers();

        Settings.NotifyBySettingName(nameof(Settings.BookmarkTimelineWidth), UpdateBookmarkWidth);
        Settings.NotifyBySettingName(nameof(Settings.BookmarkTooltipTimeInfo), UpdateBookmarkTooltip);
        Settings.NotifyBySettingName(nameof(Settings.BookmarkTimelineBrightness), UpdateBookmarkBrightness);

        LoadedDifficultySelectController.LoadedDifficultyChangedEvent += RefreshBookmarksFromLoadedDifficulty;

        BookmarksUpdated.Invoke();
    }

    private void RefreshBookmarksFromLoadedDifficulty()
    {
        foreach (var container in bookmarkContainers)
        {
            Destroy(container.gameObject);
        }

        bookmarkContainers = InstantiateBookmarkContainers();
        BookmarksUpdated.Invoke();
    }

    private List<BookmarkContainer> InstantiateBookmarkContainers()
    {
        return BeatSaberSongContainer.Instance.Map.Bookmarks.Select(bookmark =>
        {
            var container = Instantiate(bookmarkContainerPrefab, transform).GetComponent<BookmarkContainer>();
            container.name = bookmark.Name;
            container.Init(this, bookmark);
            container.RefreshPosition(timelineCanvas.sizeDelta.x + CanvasWidthOffset);
            return container;
        }).OrderBy(it => it.Data.JsonTime).ToList();
    }

    // There was a significant amount of time where bookmarks did not account for official bpm events
    // while the objects did. This ensures maps in this period display bookmarks in the correct place.
    private void ConvertBookmarkTimesFromOldDevVersions()
    {
        var bookmarksUseOfficialBpmEventsKey = BeatSaberSongContainer.Instance.Map.BookmarksUseOfficialBpmEventsKey;
        var bookmarksNeedConversion = !BeatSaberSongContainer.Instance.Map.CustomData.HasKey(bookmarksUseOfficialBpmEventsKey)
            || !BeatSaberSongContainer.Instance.Map.CustomData[bookmarksUseOfficialBpmEventsKey].IsBoolean
            || !BeatSaberSongContainer.Instance.Map.CustomData[bookmarksUseOfficialBpmEventsKey].AsBool;

        bookmarksNeedConversion &= BeatSaberSongContainer.Instance.Map.MajorVersion != 4;
        
        foreach (var bookmark in BeatSaberSongContainer.Instance.Map.Bookmarks)
        {
            if (bookmarksNeedConversion)
            {
                bookmark.SongBpmTime = bookmark.JsonTime;
            }
            else
            {
                bookmark.RecomputeSongBpmTime();
            }
        }
    }

    public void RefreshBookmarkTimelinePositions()
    {
        foreach (var bookmark in bookmarkContainers)
            bookmark.RefreshPosition(timelineCanvas.sizeDelta.x + CanvasWidthOffset);
    }

    public void RefreshBookTooltips() => UpdateBookmarkTooltip(null);
    
    private void UpdateBookmarkTooltip(object _)
    {
        foreach (BookmarkContainer bookContainer in bookmarkContainers)
        {
            bookContainer.UpdateUIText();
        }
    }

    private void UpdateBookmarkWidth(object _)
    {
        foreach (BookmarkContainer bookContainer in bookmarkContainers)
        {
            bookContainer.UpdateUIWidth();
        }
    }

    private void UpdateBookmarkBrightness(object _)
    {
        foreach (var bookmarkContainer in bookmarkContainers)
        {
            bookmarkContainer.UpdateUIColor();
        }
    }

    private void LateUpdate()
    {
        if (previousCanvasWidth != timelineCanvas.sizeDelta.x)
        {
            previousCanvasWidth = timelineCanvas.sizeDelta.x;
            RefreshBookmarkTimelinePositions();
        }
    }

    public void OnCreateNewBookmark(InputAction.CallbackContext context)
    {
        if (Atsc.IsPlaying) return;

        if (context.performed)
        {
            // Randomize color and open dialog box
            bookmarkColor.Value = Color.HSVToRGB((float)rng.NextDouble(), 0.75f, 1);
            createBookmarkDialogBox.Open();
        }
    }

    public void OnNextBookmark(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        OnNextBookmark();
    }

    public void OnPreviousBookmark(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        OnPreviousBookmark();
    }

    internal void CreateNewBookmark(string name, Color? color = null)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var newBookmark = new BaseBookmark(Atsc.CurrentJsonTime, name);

        if (color != null)
        {
            newBookmark.Color = color.Value;
        }

        AddBookmark(newBookmark);
    }

    internal void AddBookmark(BaseBookmark bookmark, bool triggerEvent = true)
    {
        var container = Instantiate(bookmarkContainerPrefab, transform).GetComponent<BookmarkContainer>();
        container.name = bookmark.Name;
        container.Init(this, bookmark);
        container.RefreshPosition(timelineCanvas.sizeDelta.x + CanvasWidthOffset);

        bookmarkContainers = bookmarkContainers.Append(container).OrderBy(it => it.Data.JsonTime).ToList();
        BeatSaberSongContainer.Instance.Map.Bookmarks = bookmarkContainers.Select(x => x.Data).ToList();
        BookmarksUpdated.Invoke();

        if (triggerEvent) BookmarkAdded?.Invoke(bookmark);
    }

    internal void DeleteBookmark(BookmarkContainer container, bool triggerEvent = true)
    {
        bookmarkContainers.Remove(container);
        BeatSaberSongContainer.Instance.Map.Bookmarks = bookmarkContainers.Select(x => x.Data).ToList();
        BookmarksUpdated.Invoke();

        if (triggerEvent) BookmarkDeleted?.Invoke(container.Data);
    }

    internal void DeleteBookmarkAtTime(float time, bool triggerEvent = true)
    {
        var container = bookmarkContainers.Find(it => Mathf.Abs(it.Data.JsonTime - time) < 0.01f);

        if (container != null)
        {
            DeleteBookmark(container, triggerEvent);
        }
    }

    internal void OnNextBookmark()
    {
        var bookmark = bookmarkContainers.Find(x => x.Data.JsonTime > Atsc.CurrentJsonTime);
        if (bookmark != null) MoveToBookmark(bookmark);
    }

    internal void OnPreviousBookmark()
    {
        var bookmark = bookmarkContainers.LastOrDefault(x => x.Data.JsonTime < Atsc.CurrentJsonTime);
        if (bookmark != null) MoveToBookmark(bookmark);
    }

    private void MoveToBookmark(BookmarkContainer bookmark)
    {
        Tipc.PointerDown(); // slightly weird but it works
        Atsc.MoveToJsonTime(bookmark.Data.JsonTime);
        Tipc.PointerUp();
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications(nameof(Settings.BookmarkTimelineWidth));
        Settings.ClearSettingNotifications(nameof(Settings.BookmarkTooltipTimeInfo));
        Settings.ClearSettingNotifications(nameof(Settings.BookmarkTimelineBrightness));

        LoadedDifficultySelectController.LoadedDifficultyChangedEvent -= RefreshBookmarksFromLoadedDifficulty;
    }

    public void OnColorBookmarkModifier(InputAction.CallbackContext context) => ShiftContext = context;
}
