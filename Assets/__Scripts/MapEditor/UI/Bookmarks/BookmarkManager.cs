﻿using System;
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
            .WithConstantAlpha(1f);

        // Enable quick submit
        createBookmarkDialogBox.OnQuickSubmit(() => CreateNewBookmark(bookmarkName.Value, bookmarkColor.Value));

        // Cancel button
        createBookmarkDialogBox.AddFooterButton(null, "PersistentUI", "cancel");

        // Submit/OK button
        createBookmarkDialogBox.AddFooterButton(
            () => CreateNewBookmark(bookmarkName.Value, bookmarkColor.Value), "PersistentUI", "ok");

        // Wait for time
        yield return new WaitForSeconds(0.1f);

        bookmarkContainers = BeatSaberSongContainer.Instance.Map.Bookmarks.Select(bookmark =>
        {
            var container = Instantiate(bookmarkContainerPrefab, transform).GetComponent<BookmarkContainer>();
            container.name = bookmark.Name;
            container.Init(this, bookmark);
            container.RefreshPosition(timelineCanvas.sizeDelta.x + CanvasWidthOffset);
            return container;
        }).OrderBy(it => it.Data.JsonTime).ToList();

        Settings.NotifyBySettingName(nameof(Settings.BookmarkTimelineWidth), UpdateBookmarkWidth);
        Settings.NotifyBySettingName(nameof(Settings.BookmarkTooltipTimeInfo), UpdateBookmarkTooltip);
        UpdateBookmarkWidth(true);

        BookmarksUpdated.Invoke();
    }

    private void UpdateBookmarkTooltip(object _)
    {
        foreach (BookmarkContainer bookContainer in bookmarkContainers)
        {
            bookContainer.UpdateUI();
        }
    }

    private void UpdateBookmarkWidth(object _)
    {
        foreach (BookmarkContainer bookContainer in bookmarkContainers)
        {
            bookContainer.UpdateUIWidth();
        }
    }

    private void LateUpdate()
    {
        if (previousCanvasWidth != timelineCanvas.sizeDelta.x)
        {
            previousCanvasWidth = timelineCanvas.sizeDelta.x;
            foreach (var bookmark in bookmarkContainers)
                bookmark.RefreshPosition(timelineCanvas.sizeDelta.x + CanvasWidthOffset);
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

        var newBookmark = Settings.Instance.Load_MapV3 ? (BaseBookmark)new V3Bookmark(Atsc.CurrentBeat, name) : new V2Bookmark(Atsc.CurrentBeat, name);

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
        var bookmark = bookmarkContainers.Find(x => x.Data.JsonTime > Atsc.CurrentBeat);
        if (bookmark != null)
        {
            Tipc.PointerDown(); // slightly weird but it works
            Atsc.MoveToTimeInBeats(bookmark.Data.JsonTime);
            Tipc.PointerUp();
        }
    }

    internal void OnPreviousBookmark()
    {
        var bookmark = bookmarkContainers.LastOrDefault(x => x.Data.JsonTime < Atsc.CurrentBeat);
        if (bookmark != null)
        {
            Tipc.PointerDown();
            Atsc.MoveToTimeInBeats(bookmark.Data.JsonTime);
            Tipc.PointerUp();
        }
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications(nameof(Settings.BookmarkTimelineWidth));
        Settings.ClearSettingNotifications(nameof(Settings.BookmarkTooltipTimeInfo));
    }

    public void OnColorBookmarkModifier(InputAction.CallbackContext context) => ShiftContext = context;
}
