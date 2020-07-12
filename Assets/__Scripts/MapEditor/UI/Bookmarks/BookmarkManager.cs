using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BookmarkManager : MonoBehaviour, CMInput.IBookmarksActions
{
    internal List<BookmarkContainer> bookmarkContainers = new List<BookmarkContainer>();
    [SerializeField] private GameObject bookmarkContainerPrefab;
    public AudioTimeSyncController atsc;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f); //Wait for time
        foreach(BeatmapBookmark bookmark in BeatSaberSongContainer.Instance.map._bookmarks)
        {
            GameObject container = Instantiate(bookmarkContainerPrefab, transform);
            container.name = bookmark._name;
            container.GetComponent<BookmarkContainer>().Init(this, bookmark);
            bookmarkContainers.Add(container.GetComponent<BookmarkContainer>());
        }   
    }

    public void OnCreateNewBookmark(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PersistentUI.Instance.ShowInputBox("Enter the name of this new Bookmark.", HandleNewBookmarkName, "New Bookmark");
        }
    }

    private void HandleNewBookmarkName(string res)
    {
        if (string.IsNullOrEmpty(res) || string.IsNullOrWhiteSpace(res)) return;
        BeatmapBookmark newBookmark = new BeatmapBookmark(atsc.CurrentBeat, res);
        GameObject container = Instantiate(bookmarkContainerPrefab, transform);
        container.name = newBookmark._name;
        container.GetComponent<BookmarkContainer>().Init(this, newBookmark);
        bookmarkContainers.Add(container.GetComponent<BookmarkContainer>());
        BeatSaberSongContainer.Instance.map._bookmarks = bookmarkContainers.Select(x => x.data).ToList();
    }

    public void OnNextBookmark(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        BookmarkContainer bookmark = bookmarkContainers.FirstOrDefault(x => x.data._time > atsc.CurrentBeat);
        if (bookmark != null) atsc.MoveToTimeInBeats(bookmark.data._time);
    }

    public void OnPreviousBookmark(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        BookmarkContainer bookmark = bookmarkContainers.LastOrDefault(x => x.data._time < atsc.CurrentBeat);
        if (bookmark != null) atsc.MoveToTimeInBeats(bookmark.data._time);
    }
}
