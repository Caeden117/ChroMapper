using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class CurrentSectionDisplay : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private BookmarkManager bookmarkManger;
    private TextMeshProUGUI textMesh;

    private Stack<BookmarkContainer> upcomingBookmarks = new Stack<BookmarkContainer>();
    private bool isPlaying;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        atsc.OnTimeChanged += OnTimeChanged;
        atsc.OnPlayToggle += OnPlayToggle;
        bookmarkManger.OnBookmarksUpdated += OnTimeChanged;
    }

    void OnDisable()
    {
        atsc.OnTimeChanged -= OnTimeChanged;
        atsc.OnPlayToggle -= OnPlayToggle;
        bookmarkManger.OnBookmarksUpdated -= OnTimeChanged;
    }

    private void OnTimeChanged()
    {
        if (isPlaying)
        {
            if (upcomingBookmarks.Count == 0)
                return;
            if (upcomingBookmarks.Peek().data._time <= atsc.CurrentBeat)
                textMesh.text = upcomingBookmarks.Pop().data._name;
        }
        else
        {
            textMesh.text = bookmarkManger.bookmarkContainers.Where(x => x.data._time <= atsc.CurrentBeat).LastOrDefault()?.data._name ?? "";
        }
    }

    private void OnPlayToggle(bool isPlaying)
    {
        this.isPlaying = isPlaying;
        upcomingBookmarks.Clear();
        foreach(BookmarkContainer container in 
            bookmarkManger.bookmarkContainers.Where(x => x.data._time > atsc.CurrentBeat).OrderByDescending(x => x.data._time))
        {
            upcomingBookmarks.Push(container);
        }
    }
}
