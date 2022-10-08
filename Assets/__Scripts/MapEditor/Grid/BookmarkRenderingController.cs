using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base.Customs;
using TMPro;
using UnityEngine;


public class BookmarkRenderingController : MonoBehaviour
{
    [SerializeField] private BookmarkManager manager;
    [SerializeField] private Transform gridBookmarksParent;

    private List<CachedBookmark> renderedBookmarks = new List<CachedBookmark>();

    private class CachedBookmark
    {
        public readonly BaseBookmark MapBookmark;
        public readonly TextMeshProUGUI Text;
        public string Name;
        public Color Color;

        public CachedBookmark(BaseBookmark baseBookmark, TextMeshProUGUI text)
        {
            MapBookmark = baseBookmark;
            Text = text;
            Name = baseBookmark.Name;
            Color = baseBookmark.Color;
        }
    }

    private void Start()
    {
        manager.BookmarksUpdated += UpdateRenderedBookmarks;
        EditorScaleController.EditorScaleChangedEvent += OnEditorScaleChange;
        Settings.NotifyBySettingName(nameof(Settings.DisplayGridBookmarks), DisplayRenderedBookmarks);
        Settings.NotifyBySettingName(nameof(Settings.GridBookmarksHasLine), RefreshBookmarkGridLine);
    }

    private void DisplayRenderedBookmarks(object _) => UpdateRenderedBookmarks();

    private void UpdateRenderedBookmarks()
    {
        var currentBookmarks = BeatSaberSongContainer.Instance.Map.Bookmarks;
        if (currentBookmarks.Count < renderedBookmarks.Count) // Removed bookmark
        {
            for (int i = renderedBookmarks.Count - 1; i >= 0; i--)
            {
                CachedBookmark bookmark = renderedBookmarks[i];
                if (!currentBookmarks.Contains(bookmark.MapBookmark))
                {
                    Destroy(bookmark.Text.gameObject);
                    renderedBookmarks.Remove(bookmark);
                    return;
                }
            }
        }
        else if (currentBookmarks.Count > renderedBookmarks.Count) // Added bookmark
        {
            foreach (BaseBookmark bookmark in currentBookmarks)
            {
                if (renderedBookmarks.All(x => x.MapBookmark != bookmark))
                {
                    TextMeshProUGUI text = CreateGridBookmark(bookmark);
                    renderedBookmarks.Add(new CachedBookmark(bookmark, text));
                }
            }
        }
        else // Edited bookmark
        {
            foreach (CachedBookmark cachedBookmark in renderedBookmarks)
            {
                string mapBookmarkName = cachedBookmark.MapBookmark.Name;
                Color mapBookmarkColor = cachedBookmark.MapBookmark.Color;

                if (cachedBookmark.Name != mapBookmarkName || cachedBookmark.Color != mapBookmarkColor)
                {
                    SetGridBookmarkNameColor(cachedBookmark.Text, mapBookmarkColor, mapBookmarkName);

                    cachedBookmark.Name = mapBookmarkName;
                    cachedBookmark.Color = mapBookmarkColor;
                }
            }
        }
    }

    private void OnEditorScaleChange(float newScale)
    {
        foreach (CachedBookmark bookmarkDisplay in renderedBookmarks)
            SetBookmarkPos(bookmarkDisplay.Text.rectTransform, bookmarkDisplay.MapBookmark.Time);
    }

    private void SetBookmarkPos(RectTransform rect, float time)
    {
        //Need anchoredPosition3D, so Z gets precisely set, otherwise text might get under lighting grid
        rect.anchoredPosition3D = new Vector3(-4.5f, time * EditorScaleController.EditorScale, 0);
    }

    private TextMeshProUGUI CreateGridBookmark(BaseBookmark baseBookmark)
    {
        GameObject obj = new GameObject("GridBookmark", typeof(TextMeshProUGUI));
        RectTransform rect = (RectTransform)obj.transform;
        rect.SetParent(gridBookmarksParent);
        SetBookmarkPos(rect, baseBookmark.Time);
        rect.sizeDelta = Vector2.one;
        rect.localRotation = Quaternion.identity;

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.font = PersistentUI.Instance.ButtonPrefab.Text.font;
        text.alignment = TextAlignmentOptions.Left;
        text.fontSize = 0.4f;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.fontMaterial.renderQueue = 3150; // Above grid and measure numbers - Below grid interface
        SetGridBookmarkNameColor(text, baseBookmark.Color, baseBookmark.Name);

        return text;
    }

    private void RefreshBookmarkGridLine(object _)
    {
        foreach (CachedBookmark cachedBookmark in renderedBookmarks)
            SetGridBookmarkNameColor(cachedBookmark.Text, cachedBookmark.Color, cachedBookmark.Name);
    }


    private void SetGridBookmarkNameColor(TextMeshProUGUI text, Color color, string name)
    {
        string hex = HEXFromColor(color, false);

        SetText();
        text.ForceMeshUpdate();

        //Here making so bookmarks with short name have still long colored rectangle on the right to the grid
        if (text.textBounds.size.x < 2) //2 is distance between notes and lighting grid
        {
            SetText((int)((2 - text.textBounds.size.x) / 0.0642f)); //Divided by 'space' character width for chosen fontSize
        }

        void SetText(int spaceNumber = 0)
        {
            string spaces = spaceNumber <= 0 ? null : new string(' ', spaceNumber);
            //<voffset> to align the bumped up text to grid, <s> to draw a line across the grid, in the end putting transparent dot, so trailing spaces don't get trimmed, 
            text.text = (Settings.Instance.GridBookmarksHasLine)
                ? $"<mark={hex}50><voffset=0.06><s> <indent=3.92> </s></voffset> {name}{spaces}<color=#00000000>.</color>"
                : $"<mark={hex}50><voffset=0.06> <indent=3.92> </voffset> {name}{spaces}<color=#00000000>.</color>";
        }
    }

    /// <summary> Returned string starts with # </summary>
    private string HEXFromColor(Color color, bool inclAlpha = true) => inclAlpha
        ? $"#{ColorUtility.ToHtmlStringRGBA(color)}"
        : $"#{ColorUtility.ToHtmlStringRGB(color)}";

    public void RefreshVisibility(float currentBeat, float beatsAhead, float beatsBehind)
    {
        foreach (var bookmarkDisplay in renderedBookmarks)
        {
            var time = bookmarkDisplay.MapBookmark.Time;
            var text = bookmarkDisplay.Text;
            var enabled = time >= currentBeat - beatsBehind && time <= currentBeat + beatsAhead;
            text.gameObject.SetActive(enabled);
        }
    }

    private void OnDestroy()
    {
        manager.BookmarksUpdated -= UpdateRenderedBookmarks;
        EditorScaleController.EditorScaleChangedEvent -= OnEditorScaleChange;
        Settings.ClearSettingNotifications(nameof(Settings.DisplayGridBookmarks));
        Settings.ClearSettingNotifications(nameof(Settings.GridBookmarksHasLine));
    }
}
