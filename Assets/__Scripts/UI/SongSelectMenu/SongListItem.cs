using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SongListItem : RecyclingListViewItem, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {

    private SongList _songList;
    
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI artist;
    [SerializeField] private TextMeshProUGUI folder;

    [SerializeField] private TextMeshProUGUI duration;
    [SerializeField] private TextMeshProUGUI bpm;
    [SerializeField] private Image favouritePreviewImage;

    [SerializeField] private Image cover;
    [SerializeField] private Sprite defaultCover;
    
    [SerializeField] private GameObject rightPanel;
    [SerializeField] private Toggle favouriteToggle;
    private Image _bg;
    private string _previousSearch = "";

    private BeatSaberSong _song;
    private static readonly Dictionary<string, WeakReference<Sprite>> Cache = new Dictionary<string, WeakReference<Sprite>>();
    private static readonly Dictionary<string, float> DurationCache = new Dictionary<string, float>();
    private static bool _hasAppliedThisFrame = false;
    
    private static string _durationCachePath;
    private static JSONObject _songCoreCache;

    private bool _ignoreToggle = false;

    private void Start()
    {
        rightPanel.SetActive(false);
        _bg = GetComponent<Image>();
        // I have sinned
        _songList = FindObjectOfType<SongList>();

        if (_songCoreCache !=  null) return;
        _durationCachePath = Path.Combine(Settings.Instance.BeatSaberInstallation, "UserData", "SongCore", "SongDurationCache.dat");
        if (!File.Exists(_durationCachePath))
        {
            _songCoreCache = new JSONObject();
            return;
        }

        try
        {
            using (var reader = new StreamReader(_durationCachePath)) {
                _songCoreCache = JSON.Parse(reader.ReadToEnd()).AsObject;
                foreach (var keyValuePair in _songCoreCache)
                {
                    DurationCache[Path.GetFullPath(keyValuePair.Key)] = keyValuePair.Value["duration"].AsFloat;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error trying to read from file {_durationCachePath}\n{e}");
        }
    }

    private string HighlightSubstring(string s, string search)
    {
        var stripped = s.StripTMPTags();
        var idx = stripped.IndexOf(search, StringComparison.InvariantCultureIgnoreCase);
        return idx >= 0 && search.Length > 0
            ? stripped.Substring(0, idx) + "<color=#ff0000ff>" + stripped.Substring(idx, search.Length) + "</color>" + stripped.Substring(idx + search.Length)
            : stripped;
    }

    public void AssignSong(BeatSaberSong song, string searchFieldText)
    {
        if (_song == song && _previousSearch == searchFieldText) return;

        StopAllCoroutines();

        _previousSearch = searchFieldText;
        _song = song;
        var songName = HighlightSubstring(song.songName, searchFieldText);
        var artistName = HighlightSubstring(song.songAuthorName, searchFieldText);

        title.text = $"{songName} <size=50%><i>{song.songSubName.StripTMPTags()}</i></size>";
        artist.text = artistName;
        folder.text = song.directory;

        duration.text = "-:--";
        bpm.text = $"{song.beatsPerMinute:N0}";

        _ignoreToggle = true;
        favouriteToggle.isOn = _song.IsFavourite;
        favouritePreviewImage.gameObject.SetActive(_song.IsFavourite);
        _ignoreToggle = false;

        StartCoroutine(LoadImage());
        StartCoroutine(LoadDuration());
    }

    private IEnumerator LoadImage()
    {
        var fullPath = Path.Combine(_song.directory, _song.coverImageFilename);

        if (Cache.TryGetValue(fullPath, out var spriteRef) && spriteRef.TryGetTarget(out var existingSprite))
        {
            cover.sprite = existingSprite;
            yield break;
        }

        cover.sprite = defaultCover;
        if (!File.Exists(fullPath)) yield break;

        var www = UnityWebRequestTexture.GetTexture($"file:///{Uri.EscapeDataString($"{fullPath}")}");
        yield return www.SendWebRequest();

        // Copying the texture generates mipmaps for better scaling
        var newTex = ((DownloadHandlerTexture)www.downloadHandler).texture;

        newTex.wrapMode = TextureWrapMode.Clamp;
        
        // Only allow one sprite to be created per frame to reduce stuttering
        while (_hasAppliedThisFrame)
            yield return new WaitForEndOfFrame();

        _hasAppliedThisFrame = true;

        var sprite = Sprite.Create(newTex, new Rect(0, 0, newTex.width, newTex.height), new Vector2(0, 0), 100f);
        cover.sprite = sprite;
        Cache[fullPath] = new WeakReference<Sprite>(sprite);
    }

    private void Update()
    {
        _hasAppliedThisFrame = false;
    }

    private void SetDuration(string path, float length) => SetDuration(this, path, length);

    public static void SetDuration(MonoBehaviour crTarget, string path, float length)
    {
        var songCoreCacheObj = _songCoreCache.GetValueOrDefault(path, new JSONObject
        {
            ["id"] = "CMCachedDuration"
        });
        songCoreCacheObj["duration"] = length;
        _songCoreCache.Add(path, songCoreCacheObj);
        crTarget.StartCoroutine(SaveCachedDurations());

        DurationCache[path] = length;
        if (crTarget is SongListItem item)
        {
            item.SetDuration(length);
        }
    }

    private static bool _saveRunning = false;
    private static IEnumerator SaveCachedDurations()
    {
        if (_saveRunning) yield break;
        _saveRunning = true;

        if (!File.Exists(_durationCachePath))
            Directory.CreateDirectory(Path.GetDirectoryName(_durationCachePath) ?? throw new InvalidOperationException("Directory was null?"));

        yield return new WaitForSeconds(3);
        using (var writer = new StreamWriter(_durationCachePath, false))
            writer.Write(_songCoreCache.ToString());

        _saveRunning = false;
    }

    private void SetDuration(float length)
    {
        var totalSeconds = Mathf.RoundToInt(length);
        var mins = totalSeconds / 60;
        var seconds = totalSeconds % 60;

        duration.text = $"{mins}:{seconds:D2}";
    }
    
    private IEnumerator LoadDuration()
    {
        var cacheKey = Path.GetFullPath(_song.directory);
        var fullPath = Path.Combine(_song.directory, _song.songFilename);

        if (!File.Exists(fullPath)) yield break;

        yield return null;

        if (DurationCache.TryGetValue(cacheKey, out var cachedDuration) && cachedDuration >= 0)
        {
            SetDuration(cachedDuration);
            yield break;
        }

        var oggLength = GetLengthFromOgg(fullPath);
        if (oggLength >= 0)
        {
            SetDuration(cacheKey, oggLength);
            yield break;
        }

        // Fallback just loads the song via unity
        var extension = _song.songFilename.Contains(".") ? Path.GetExtension(_song.songFilename.ToLower()).Replace(".", "") : "";

        if (!string.IsNullOrEmpty(extension) && SongInfoEditUI.ExtensionToAudio.ContainsKey(extension))
        {
            var audioType = SongInfoEditUI.ExtensionToAudio[extension];

            yield return null;

            var www = UnityWebRequestMultimedia.GetAudioClip($"file:///{Uri.EscapeDataString($"{fullPath}")}", audioType);
            yield return www.SendWebRequest();
            
            var clip = DownloadHandlerAudioClip.GetContent(www);
            if (clip == null)
                yield break;

            SetDuration(cacheKey, clip.length);
        }
    }

    private static bool FindBytes(Stream fs, BinaryReader br, byte[] bytes, int searchLength)
    {
        for (var i = 0; i < searchLength; i++)
        {
            var b = br.ReadByte();
            if (b != bytes[0]) continue;
            var by = br.ReadBytes(bytes.Length - 1);
            // hardcoded 6 bytes compare, is fine because all inputs used are 6 bytes
            // bitwise AND the last byte to read only the flag bit for lastSample searching
            // shouldn't cause issues finding rate, hopefully
            if (by[0] == bytes[1] && by[1] == bytes[2] && by[2] == bytes[3] && by[3] == bytes[4] && (by[4] & bytes[5]) == bytes[5]) return true;
            var index = Array.IndexOf(by, bytes[0]);
            if (index != -1)
            {
                fs.Position += index - (bytes.Length - 1);
                i += index;
            }
            else
            {
                i += bytes.Length - 1;
            }
        }
        return false;
    }

    private static readonly byte[] OggBytes = new byte[] { 0x4F, 0x67, 0x67, 0x53, 0x00, 0x04 };
    private static readonly byte[] VorbisBytes = new byte[] { 0x76, 0x6F, 0x72, 0x62, 0x69, 0x73 };
    public static float GetLengthFromOgg(string oggFile)
    {
        using (var fs = File.OpenRead(oggFile))
        using (var br = new BinaryReader(fs, Encoding.ASCII))
        {
            //Skip Capture Pattern
            fs.Position = 24;

            if (!FindBytes(fs, br, VorbisBytes, 256))
            {
                Debug.Log($"Could not find rate for {oggFile}");
                return -1;
            }

            fs.Position += 5;
            var rate = br.ReadInt32();
            long lastSample = -1;

            /*
             * this finds the last occurrence of OggS in the file by checking for a bit flag (0x04)
             * reads in blocks determined by seekBlockSize
             * 6144 does not add significant overhead and speeds up the search significantly
             */
            const int seekBlockSize = 6144;
            const int seekTries = 10; // 60 KiB should be enough for any sane ogg file
            for (var i = 0; i < seekTries; i++)
            {
                var seekPos = (i + 1) * seekBlockSize * -1;
                var overshoot = Math.Max((int)(-seekPos - fs.Length), 0);
                if (overshoot >= seekBlockSize)
                    break;

                fs.Seek(seekPos + overshoot, SeekOrigin.End);
                if (!FindBytes(fs, br, OggBytes, seekBlockSize - overshoot)) continue;

                lastSample = br.ReadInt64();
                break;
            }

            if (lastSample == -1)
            {
                Debug.Log($"Could not find lastSample for {oggFile}");
                return -1;
            }

            return lastSample / (float) rate;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rightPanel.SetActive(true);
        _bg.color = new Color(0.35f, 0.35f, 0.36f, 1);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rightPanel.SetActive(false);
        _bg.color = new Color(0.31f, 0.31f, 0.31f, 1);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (BeatSaberSongContainer.Instance != null && _song != null) BeatSaberSongContainer.Instance.SelectSongForEditing(_song);
    }

    public void OnFavourite(bool isFavourite)
    {
        if (_ignoreToggle) return;

        _songList.RemoveSong(_song);
        _song.IsFavourite = isFavourite;
        favouritePreviewImage.gameObject.SetActive(isFavourite);
        _songList.AddSong(_song);
    }
}
