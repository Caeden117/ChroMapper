using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using SimpleJSON;
using UnityEngine;

public class Settings
{

    private static Settings instance;
    public static Settings Instance => instance ??= Load();

    public string BeatSaberInstallation = "";
    public string CustomSongsFolder => Path.Combine(BeatSaberInstallation, "Beat Saber_Data", "CustomLevels");
    public string CustomWIPSongsFolder => Path.Combine(BeatSaberInstallation, "Beat Saber_Data", "CustomWIPLevels");
    public string CustomPlatformsFolder => Path.Combine(BeatSaberInstallation, "CustomPlatforms");

    public bool DiscordRPCEnabled = true;
    public float EditorScale = 4;
    public bool EditorScaleBPMIndependent = false;
    public int ChunkDistance = 5;
    public int AutoSaveInterval = 5;
    public bool InvertNoteControls = false; // Hidden setting, does nothing
    public int Waveform = 1;
    public CountersPlusSettings CountersPlus = new CountersPlusSettings();
    public bool PickColorFromChromaEvents = false;
    public bool PlaceChromaColor = false;
    public bool PlaceOnlyChromaEvents = false; // Hidden setting, does nothing
    public bool BongoBoye = false;
    public int BongoCat = -1;
    public bool AutoSave = true;
    public float Volume = 1;
    public float MetronomeVolume = 0;
    public float SongVolume = 1;
    public bool NodeEditor_Enabled = true;
    public bool NodeEditor_UseKeybind = true;
    public float PostProcessingIntensity = 0.1f;
    public bool DarkTheme = true;
    public bool BoxSelect = true;
    public bool DontPlacePerfectZeroDurationWalls = true;
    public float Camera_MovementSpeed = 15;
    public float Camera_MouseSensitivity = 2;
    public bool EmulateChromaLite = true; //To get Chroma RGB lights
    public bool EmulateChromaAdvanced = true; //Ring propagation and other advanced chroma features
    public bool RotateTrack = true; // 360/90 mode
    public bool HighlightLastPlacedNotes = false; // Hidden setting, does nothing
    public bool InvertPrecisionScroll = false;
    public bool Reminder_Loading360Levels = true;
    public bool Reminder_SettingsFailed = true;
    public bool AdvancedShit = false;
    public bool InstantEscapeMenuTransitions = false;
    public bool ChromaticAberration = true;
    public int Offset_Spawning = 4;
    public int Offset_Despawning = 1;
    public int NoteHitSound = 0;
    public float NoteHitVolume = 0.5f;
    public float PastNotesGridScale = 0.5f;
    public float CameraFOV = 60f;
    public int CameraAA = 0;
    public bool WaveformWorkflow = true;
    public bool Load_Events = true;
    public bool Load_Notes = true;
    public bool Load_Obstacles = true;
    public bool Load_Others = true;
    public bool ShowMoreAccurateFastWalls = false;
    public int TimeValueDecimalPrecision = 3;
    public bool Ding_Red_Notes = true;
    public bool Ding_Blue_Notes = true;
    public bool Ding_Bombs = false;
    public bool MeasureLinesShowOnTop = false;
    public bool Reflections = true;
    public bool HighQualityBloom = true;
    public bool ColorFakeWalls = true;
    public bool InvertScrollTime = false;
    public bool PrecisionPlacementGrid = false;
    public bool NoteJumpSpeedForEditorScale = false;
    public bool VisualizeChromaGradients = true;
    public bool VisualizeChromaAlpha = true;
    public bool SimpleBlocks = false;
    public bool HelpfulLoadingMessages = false;
    public bool Reset360DisplayOnCompleteTurn = true;
    public string Language = "en";
    public bool HighContrastGrids = false;
    public float GridTransparency = 0f;
    public float UIScale = 1;
    public CameraPosition[] SavedPositions = new CameraPosition[8];
    public bool Reminder_UnsupportedEditorOffset = true;
    public bool PyramidEventModels = false;
    public EventModelType EventModel = EventModelType.Block;
    public int ReleaseChannel = 0;
    public string ReleaseServer = "https://cm.topc.at";
    public int DSPBufferSize = 10;
    public bool QuickNoteEditing = false;
    public int AudioLatencyCompensation = 0;
    public int MaximumFPS = 9999;
    public bool VSync = true;

    public int NodeEditorTextSize = 10;
    public int NodeEditorSize = 10;

    public int CursorPrecisionA = 1;
    public int CursorPrecisionB = 1;

    public string LastLoadedMap = "";
    public string LastLoadedChar = "";
    public string LastLoadedDiff = "";

    public int LastSongSortType = (int)SongList.SongSortType.Name;

    public static Dictionary<string, FieldInfo> AllFieldInfos = new Dictionary<string, FieldInfo>();
    public static Dictionary<string, object> NonPersistentSettings = new Dictionary<string, object>();

    private static readonly Dictionary<string, Action<object>> nameToActions = new Dictionary<string, Action<object>>();

    private static Settings Load()
    {
        //Fixes weird shit regarding how people write numbers (20,35 VS 20.35), causing issues in JSON
        //This should be thread-wide, but I have this set throughout just in case it isnt.
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        var settingsFailed = false;

        var settings = new Settings();
        var type = settings.GetType();
        var infos = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);

        // Use default settings if config does not exist
        if (!File.Exists(Application.persistentDataPath + "/ChroMapperSettings.json"))
        {
            foreach (var info in infos)
            {
                if (!(info is FieldInfo field)) continue;
                AllFieldInfos.Add(field.Name, field);
            }
            return settings;
        }

        using (var reader = new StreamReader(Application.persistentDataPath + "/ChroMapperSettings.json"))
        {
            var mainNode = JSON.Parse(reader.ReadToEnd());

            foreach (var info in infos)
            {
                try
                {
                    if (!(info is FieldInfo field)) continue;

                    AllFieldInfos.Add(field.Name, field);

                    var nodeValue = mainNode[field.Name];

                    if (nodeValue != null)
                    {
                        if (nodeValue is JSONArray arr)
                        {
                            var newArr = Array.CreateInstance(field.FieldType.GetElementType(), arr.Count);

                            for (var i = 0; i < arr.Count; i++)
                            {
                                if (arr[i] == null) continue;

                                var elementType = field.FieldType.GetElementType();
                                var element = Activator.CreateInstance(elementType);

                                if (element is IJsonSetting elementJSON)
                                {
                                    elementJSON.FromJson(arr[i]);
                                    newArr.SetValue(elementJSON, i);
                                }
                                else
                                {
                                    newArr.SetValue(Convert.ChangeType(arr[i], elementType), i);
                                }
                            }
                            field.SetValue(settings, newArr);
                        }
                        else if (field.FieldType.BaseType == typeof(Enum))
                        {
                            var parsedEnumValue = Enum.Parse(field.FieldType, nodeValue);
                            field.SetValue(settings, parsedEnumValue);
                        }
                        else if (typeof(IJsonSetting).IsAssignableFrom(field.FieldType))
                        {
                            var elementJSON = (IJsonSetting)Activator.CreateInstance(field.FieldType);
                            elementJSON.FromJson(nodeValue);
                            field.SetValue(settings, elementJSON);
                        }
                        else
                        {
                            field.SetValue(settings, Convert.ChangeType(nodeValue.Value, field.FieldType));
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Setting {info.Name} failed to load.\n{e}");
                    settingsFailed = true;
                }
            }
        }

        if (settingsFailed)
        {
            PersistentUI.Instance.StartCoroutine(ShowFailedDialog());
        }

        JSONNumber.CapNumbersToDecimals = true;
        JSONNumber.DecimalPrecision = settings.TimeValueDecimalPrecision;

        settings.UpdateOldSettings();

        return settings;
    }

    public static System.Collections.IEnumerator ShowFailedDialog()
    {
        // Need to wait until the settings instance has been created, just put ourselves at the end of the event loop
        yield return new WaitForEndOfFrame();
        PersistentUI.Instance.ShowDialogBox("PersistentUI", "settings.loadfailed",
                Instance.HandleFailedReminder, PersistentUI.DialogBoxPresetType.OkIgnore, new object[] { Application.persistentDataPath });
    }

    private void HandleFailedReminder(int res) => Reminder_SettingsFailed = res == 0;

    private void UpdateOldSettings()  //Put code in here to transfer any settings that are fundamentally changed and require conversion from an old setting to a new setting
    {
        if (PyramidEventModels)
        {
            EventModel = EventModelType.Pyramid;
            PyramidEventModels = false;
        }

        if (BongoBoye)
        {
            BongoCat = 0;
            BongoBoye = false;
        }
    }

    public void Save()
    {
        var mainNode = new JSONObject();
        var type = GetType();
        var infos = type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x is FieldInfo)
            .OrderBy(x => x.Name)
            .Cast<FieldInfo>()
            .ToArray();

        foreach (var info in infos)
        {
            var val = info.GetValue(this);

            if (info.FieldType.IsArray)
            {
                var arr = new JSONArray();
                foreach (var item in (object[])val)
                {
                    if (item == null)
                    {
                        arr.Add(null);
                    }
                    else if (item is IJsonSetting setting)
                    {
                        arr.Add(setting.ToJson());
                    }
                    else
                    {
                        arr.Add(item.ToString());
                    }
                }
                mainNode[info.Name] = arr;
            }
            else if (val is IJsonSetting jsonVal)
            {
                mainNode[info.Name] = jsonVal.ToJson();
            }
            else if (val != null)
            {
                mainNode[info.Name] = val.ToString();
            }
        }

        using (var writer = new StreamWriter(Application.persistentDataPath + "/ChroMapperSettings.json", false))
            writer.Write(mainNode.ToString(2));
    }

    public static Dictionary<string, Type> GetAllFieldInfos()
    {
        var infoNames = new Dictionary<string, Type>();
        var type = typeof(Settings);
        var infos = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);

        foreach (var info in infos)
        {
            if (!(info is FieldInfo field)) continue;
            infoNames.Add(field.Name, field.FieldType);
        }
        return infoNames;
    }

    public static void ApplyOptionByName(string name, object value)
    {
        if (AllFieldInfos.TryGetValue(name, out var fieldInfo))
        {
            fieldInfo.SetValue(Instance, value);
            ManuallyNotifySettingUpdatedEvent(name, value);
        }
        else
        {
            throw new ArgumentException($"Setting {name} does not exist.");
        }
    }

    /// <summary>
    /// Attach an <see cref="Action"/> to an ID that will be triggered when a setting associated with that ID has been changed.
    /// This is purposefully designed to accept IDs that are not defined in the <see cref="Settings"/> object.
    /// </summary>
    public static void NotifyBySettingName(string name, Action<object> callback)
    {
        if (nameToActions.ContainsKey(name) && callback != null)
        {
            nameToActions[name] += callback;
        }
        else if (!nameToActions.ContainsKey(name) && callback != null)
        {
            var newBoy = new Action<object>(callback);
            nameToActions.Add(name, newBoy);
        }
    }

    /// <summary>
    /// Clear all <see cref="Action"/>s associated with the given ID.
    /// </summary>
    public static void ClearSettingNotifications(string name) => nameToActions.Remove(name);

    /// <summary>
    /// Manually trigger an event given an ID and the object to pass through.
    /// </summary>
    public static void ManuallyNotifySettingUpdatedEvent(string name, object value)
    {
        if (NonPersistentSettings.ContainsKey(name)) NonPersistentSettings[name] = value;
        if (nameToActions.TryGetValue(name, out var boy)) boy?.Invoke(value);
    }

    public static bool ValidateDirectory(Action<string> errorFeedback = null)
    {
        if (!Directory.Exists(Instance.BeatSaberInstallation))
        {
            errorFeedback?.Invoke("validate.missing");
            return false;
        }
        if (!Directory.Exists(Instance.CustomSongsFolder))
        {
            errorFeedback?.Invoke("validate.nofolders");
            return false;
        }
        if (!Directory.Exists(Instance.CustomWIPSongsFolder))
        {
            errorFeedback?.Invoke("validate.nowip");
            return false;
        }
        return true;
    }
}
