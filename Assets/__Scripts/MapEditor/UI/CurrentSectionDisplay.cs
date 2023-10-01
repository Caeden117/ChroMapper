using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class CurrentSectionDisplay : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private BookmarkManager bookmarkManger;

    private readonly Stack<BookmarkContainer> upcomingBookmarks = new Stack<BookmarkContainer>();
    private bool isPlaying;
    private TextMeshProUGUI textMesh;

    private void Awake() => textMesh = GetComponent<TextMeshProUGUI>();

    private void OnEnable()
    {
        atsc.TimeChanged += OnTimeChanged;
        atsc.PlayToggle += OnPlayToggle;
        bookmarkManger.BookmarksUpdated += OnTimeChanged;
    }

    private void OnDisable()
    {
        atsc.TimeChanged -= OnTimeChanged;
        atsc.PlayToggle -= OnPlayToggle;
        bookmarkManger.BookmarksUpdated -= OnTimeChanged;
    }

    private void OnTimeChanged()
    {
        if (isPlaying)
        {
            if (upcomingBookmarks.Count == 0)
                return;
            if (upcomingBookmarks.Peek().Data.JsonTime <= atsc.CurrentJsonTime)
                textMesh.text = upcomingBookmarks.Pop().Data.Name;
        }
        else
        {
            var lastBookmark = bookmarkManger.bookmarkContainers.FindLast(x => x.Data.JsonTime <= atsc.CurrentJsonTime);

            textMesh.text = lastBookmark != null ? lastBookmark.Data.Name : "";
        }
    }

    private void OnPlayToggle(bool isPlaying)
    {
        this.isPlaying = isPlaying;
        upcomingBookmarks.Clear();
        foreach (var container in
            bookmarkManger.bookmarkContainers.Where(x => x.Data.JsonTime > atsc.CurrentJsonTime)
                .OrderByDescending(x => x.Data.JsonTime))
        {
            upcomingBookmarks.Push(container);
        }
    }
}
