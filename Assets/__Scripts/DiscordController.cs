using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Discord;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class DiscordController : MonoBehaviour
{
    public static bool IsActive = true;
    [SerializeField] private TextAsset clientIDTextAsset;

    private readonly float discordUpdateMinutes = 1;
    private Activity activity;
    public ActivityManager ActivityManager;

    public Discord.Discord Discord;
    private Coroutine mapPresenceRoutine;
    private PlatformDescriptor platform;

    // Start is called before the first frame update
    private void Start()
    {
        if (Settings.Instance.DiscordRPCEnabled == false) return;
        try
        {
            if (long.TryParse(clientIDTextAsset.text, out var discordClientID) &&
                Application.internetReachability != NetworkReachability.NotReachable)
            {
                Discord = new Discord.Discord(discordClientID, (ulong)CreateFlags.NoRequireDiscord);
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

    private void LoadPlatform(PlatformDescriptor descriptor) => platform = descriptor;

    private void SceneUpdated(Scene from, Scene to)
    {
        if (mapPresenceRoutine != null) StopCoroutine(mapPresenceRoutine);
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
                details =
                    $"Editing {BeatSaberSongContainer.Instance.Song.SongName}" + //Editing TTFAF (Standard ExpertPlus)
                    $" ({BeatSaberSongContainer.Instance.DifficultyData.ParentBeatmapSet.BeatmapCharacteristicName} " +
                    $"{BeatSaberSongContainer.Instance.DifficultyData.Difficulty})";
                break;
            case "04_Options":
                details = "Editing ChroMapper options";
                break;
        }

        activity = new Activity
        {
            Details = details,
            State = state,
            Timestamps =
                new ActivityTimestamps {Start = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds},
            Assets = new ActivityAssets
            {
                SmallImage = "newlogo",
                SmallText = "ChroMapper Closed Beta",
                LargeImage = "newlogo_glow",
                LargeText = "In Menus"
            }
        };
        if (to.name == "03_Mapper")
            mapPresenceRoutine = StartCoroutine(MapperPresenceTick());
        else UpdatePresence();
    }

    private IEnumerator MapperPresenceTick()
    {
        while (true)
        {
            yield return new WaitUntil(() => platform != null);
            var randomStates = new List<string>
            {
                $"{BeatSaberSongContainer.Instance.Map.Obstacles.Count} Obstacles in Map",
                $"{BeatSaberSongContainer.Instance.Map.Notes.Count} Notes in Map",
                $"{BeatSaberSongContainer.Instance.Map.Events.Count} Events in Map"
            };
            if (NodeEditorController.IsActive) randomStates.Add("Using Node Editor");
            if (BeatSaberSongContainer.Instance.Map.Events.Any(x => x.Value >= ColourManager.RGBAlt))
                randomStates.Add("Now with Chroma RGB!");
            activity.State = randomStates[Random.Range(0, randomStates.Count)];

            var platformName = platform.gameObject.name.Substring(0,
                platform.gameObject.name.IndexOf("(Clone)", StringComparison.Ordinal));
            var actualPlatformName = SongInfoEditUI.VanillaEnvironments
                .Find(x => x.JsonName == BeatSaberSongContainer.Instance.Song.EnvironmentName).HumanName;
            activity.Assets.LargeImage = string.Join("", platformName.Split(' ')).ToLower();
            activity.Assets.LargeText = actualPlatformName;

            UpdatePresence();
            yield return new WaitForSeconds(discordUpdateMinutes * 60);
        }
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
            , null, PersistentUI.DialogBoxPresetType.Ok, new object[] {msg});
        IsActive = false;
    }
}
