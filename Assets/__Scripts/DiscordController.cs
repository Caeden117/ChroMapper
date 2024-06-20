using System;
using Discord;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DiscordController : MonoBehaviour
{
    public static bool IsActive = true;
    public static ImageManager ImageManager = null;
    public static UserManager UserManager = null;

    public ActivityManager ActivityManager;
    public Discord.Discord Discord;

    private Activity activity;

    [SerializeField] private TextAsset clientIDTextAsset;

    // Start is called before the first frame update
    private void Start()
    {
        if (Settings.Instance.DiscordRPCEnabled == false)
        {
            IsActive = false;
            return;
        }

        try
        {
            if (long.TryParse(clientIDTextAsset.text, out var discordClientID) &&
                Application.internetReachability != NetworkReachability.NotReachable)
            {
                Discord = new Discord.Discord(discordClientID, (ulong)CreateFlags.NoRequireDiscord);
                ImageManager = Discord.GetImageManager();
                UserManager = Discord.GetUserManager();
                ActivityManager = Discord.GetActivityManager();
                ActivityManager.ClearActivity(res => { });
                SceneManager.activeSceneChanged += SceneUpdated;
                LoadInitialMap.PlatformLoadedEvent += LoadPlatform;
            }
            else
            {
                HandleException("No internet connection, or invalid Client ID.");
            }
        }
        catch (ResultException result)
        {
            HandleException($"{result.Message} (Perhaps Discord is not open?)");
        }
        catch (DllNotFoundException e)
        {
            HandleException($"{e.Message} Dll missing?");
        }
    }

    // Update is called once per frame
    private void Update()
    {
        try
        {
            if (IsActive) Discord?.RunCallbacks();
        }
        catch (ResultException resultException)
        {
            HandleException(resultException.Message);
        }
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= SceneUpdated;
        LoadInitialMap.PlatformLoadedEvent -= LoadPlatform;
    }

    private void OnApplicationQuit() => Discord?.Dispose();

    private void LoadPlatform(PlatformDescriptor platform)
    {
        var platformDiscordID = platform.gameObject.name
            .Replace("(Clone)", "")
            .Replace(" ", "")
            .ToLowerInvariant()
            .Trim();

        activity.Assets.LargeImage = platformDiscordID;

        var jsonEnvironmentName = BeatSaberSongContainer.Instance.Song.EnvironmentName;

        var platformName = SongInfoEditUI.VanillaEnvironments
            .Find(x => x.JsonName == jsonEnvironmentName)?.HumanName ?? jsonEnvironmentName;
        activity.Assets.LargeText = platformName;

        UpdatePresence();
    }

    private void SceneUpdated(Scene from, Scene to)
    {
        StopAllCoroutines();

        var details = "Invalid!";
        var state = "";

        switch (to.name)
        {
            case "00_FirstBoot":
                details = "Selecting install folder...";
                break;
            case "01_SongSelectMenu":
                details = "Viewing song list.";
                break;
            case "02_SongEditMenu":
                details = BeatSaberSongContainer.Instance.Song.SongName;
                state = "Viewing song info.";
                break;
            case "03_Mapper":
                var songContainer = BeatSaberSongContainer.Instance;

                var song = songContainer.Song;
                var diff = songContainer.DifficultyData;
                var beatmapSet = diff.ParentBeatmapSet;

                details = $"Editing {song.SongName}";
                state = $"{beatmapSet.BeatmapCharacteristicName} {diff.Difficulty}";
                break;
            case "04_Options":
                details = "Editing ChroMapper options";
                break;
        }

        activity = new Activity
        {
            Details = details,
            State = state,
            Timestamps = new ActivityTimestamps
            { 
                Start = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds
            },
            Assets = new ActivityAssets
            {
                SmallImage = "newlogo",
                SmallText = $"ChroMapper v{Application.version}",
                LargeImage = "newlogo_glow",
                LargeText = "In Menus"
            }
        };

        UpdatePresence();
    }

    private void UpdatePresence()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable) return;

        ActivityManager?.UpdateActivity(activity, res =>
        {
            if (res == Result.Ok) Debug.Log("Discord Presence updated!");
            else Debug.LogWarning($"Discord Presence failed! {res}");
        });
    }

    private void HandleException(string msg)
    {
        PersistentUI.Instance.ShowDialogBox(
            "PersistentUI", "discord.error"
            , null, PersistentUI.DialogBoxPresetType.Ok, new object[] { msg });
        IsActive = false;
    }
}
