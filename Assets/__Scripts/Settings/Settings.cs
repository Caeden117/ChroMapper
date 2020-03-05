using SimpleJSON;
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using System.Globalization;
using __Scripts.MapEditor.Hit_Sounds;

public class Settings {

    private static Settings _instance;
    public static Settings Instance => _instance ?? (_instance = Load());

    public string BeatSaberInstallation = "";
    public string CustomSongsFolder => ConvertToDirectory(BeatSaberInstallation + "/Beat Saber_Data/CustomLevels");
    public string CustomWIPSongsFolder => ConvertToDirectory(BeatSaberInstallation + "/Beat Saber_Data/CustomWIPLevels");
    public string CustomPlatformsFolder => ConvertToDirectory(BeatSaberInstallation + "/CustomPlatforms");
    public bool DiscordRPCEnabled = true;
    public bool OSC_Enabled = false;
    public string OSC_IP = "127.0.0.1";
    public string OSC_Port = "8080";
    public int EditorScale = 4;
    public int ChunkDistance = 5;
    public int AutoSaveInterval = 5;
    public int InitialLoadBatchSize = 100;
    public bool InvertNoteControls = false;
    public bool WaveformGenerator = false;
    public bool CountersPlus = false;
    public bool PlaceChromaEvents = false;
    public bool PickColorFromChromaEvents = false;
    public bool PlaceOnlyChromaEvents = false;
    public bool BongoBoye = false;
    public bool AutoSave = true;
    public float Volume = 1;
    public float MetronomeVolume = 0;
    public bool NodeEditor_Enabled = false;
    public bool NodeEditor_UseKeybind = false;
    public float PostProcessingIntensity = 1;
    public bool Reminder_SavingCustomEvents = true;
    public bool DarkTheme = false;
    public bool BoxSelect = false;
    public bool DontPlacePerfectZeroDurationWalls = true;
    public float Camera_MovementSpeed = 15;
    public float Camera_MouseSensitivity = 2;
    public bool EmulateChromaLite = true; //To get Chroma RGB lights
    public bool EmulateChromaAdvanced = true; //Ring propagation and other advanced chroma features
    public bool RotateTrack = true; // 360/90 mode
    public bool HighlightLastPlacedNotes = false;
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
    public bool WaveformWorkflow = true;
    public bool Load_Events = true;
    public bool Load_Notes = true;
    public bool Load_Obstacles = true;
    public bool Load_Others = true;
    public bool ShowMoreAccurateFastWalls = false;

    private static Settings Load()
    {
        //Fixes weird shit regarding how people write numbers (20,35 VS 20.35), causing issues in JSON
        //This should be thread-wide, but I have this set throughout just in case it isnt.
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        bool settingsFailed = false;

        Settings settings = new Settings();
        if (!File.Exists(Application.persistentDataPath + "/ChroMapperSettings.json")) return settings;
        using (StreamReader reader = new StreamReader(Application.persistentDataPath + "/ChroMapperSettings.json")) //todo: save as object
        {
            JSONNode mainNode = JSON.Parse(reader.ReadToEnd());
            Type type = settings.GetType();
            MemberInfo[] infos = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            foreach (MemberInfo info in infos)
            {
                try
                {
                    if (!(info is FieldInfo field)) continue;
                    if (mainNode[field.Name] != null)
                        field.SetValue(settings, Convert.ChangeType(mainNode[field.Name].Value, field.FieldType));
                }catch(Exception e)
                {
                    Debug.LogWarning($"Setting {info.Name} failed to load.\n{e}");
                    settingsFailed = true;
                }
            }
        }
        if (settingsFailed)
        {
            PersistentUI.Instance.ShowDialogBox("Some ChroMapper settings failed to load.\n\n" +
                "If this dialog box keeps showing up when launching ChroMapper, try deleting your Configuration file located in:\n" +
                $"{Application.persistentDataPath}/ChroMapperSettings.json",
                Instance.HandleFailedReminder, "Ok", "Don't Remind Me");
        }
        return settings;
    }

    private void HandleFailedReminder(int res)
    {
        Reminder_SettingsFailed = res == 0;
    }

    public void Save()
    {
        JSONObject mainNode = new JSONObject();
        Type type = GetType();
        FieldInfo[] infos = type.GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(x => x is FieldInfo).OrderBy(x => x.Name).Cast<FieldInfo>().ToArray();
        foreach (FieldInfo info in infos) mainNode[info.Name] = info.GetValue(this).ToString();
        using (StreamWriter writer = new StreamWriter(Application.persistentDataPath + "/ChroMapperSettings.json", false))
            writer.Write(mainNode.ToString(2));
    }

    public static bool ValidateDirectory(Action<string> errorFeedback = null) {
        if (!Directory.Exists(Instance.BeatSaberInstallation)) {
            errorFeedback?.Invoke("That folder does not exist!");
            return false;
        }
        if (!Directory.Exists(Instance.CustomSongsFolder)) {
            errorFeedback?.Invoke("No \"Beat Saber_Data\" or \"CustomLevels\" folder was found at chosen location!");
            return false;
        }
        if (!Directory.Exists(Instance.CustomWIPSongsFolder))
        {
            errorFeedback?.Invoke("No \"CustomWIPLevels\" folder was found at chosen location!");
            return false;
        }
        return true;
    }

    public static string ConvertToDirectory(string s) => s.Replace('\\', '/');
}
