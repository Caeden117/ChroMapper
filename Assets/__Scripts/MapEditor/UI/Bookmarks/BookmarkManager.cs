using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BookmarkManager : MonoBehaviour
{
    private List<BookmarkContainer> bookmarkContainers = new List<BookmarkContainer>();
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
        }   
    }
}
