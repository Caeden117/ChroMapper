using SimpleJSON;
using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;

public class Settings {

    private static Settings _instance;
    public static Settings Instance { get
        {
            if (_instance == null) _instance = Load();
            return _instance;
        }
    }

    public string BeatSaberInstallation = "";
    public string CustomSongsFolder => ConvertToDirectory(BeatSaberInstallation + "/Beat Saber_Data/CustomLevels");
    public string CustomWIPSongsFolder => ConvertToDirectory(BeatSaberInstallation + "/Beat Saber_Data/CustomWIPLevels");
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
    public bool NodeEditor_Enabled = false;
    public bool NodeEditor_UseKeybind = false;
    public float PostProcessingIntensity = 1;
    public bool Saving_CustomEventsSchemaReminder = true;
    public bool DarkTheme = false;
    public bool BoxSelect = false;
    public bool DontPlacePerfectZeroDurationWalls = true;
    public float Camera_MovementSpeed = 15;
    public float Camera_MouseSensitivity = 2;
    public bool EmulateChromaLite = true; //To get Chroma RGB lights
    public bool EmulateChromaAdvanced = true; //Ring propagation and other advanced chroma features

    private static Settings Load()
    {
        Settings settings = new Settings();
        if (!File.Exists(Application.persistentDataPath + "/ChroMapperSettings.json"))
            Debug.Log("Settings file doesn't exist! Skipping loading...");
        else
        {
            using (StreamReader reader = new StreamReader(Application.persistentDataPath + "/ChroMapperSettings.json"))
            {
                JSONNode mainNode = JSON.Parse(reader.ReadToEnd());
                Type type = settings.GetType();
                MemberInfo[] infos = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
                foreach (MemberInfo info in infos)
                {
                    if (!(info is FieldInfo field)) continue;
                    if (mainNode[field.Name] != null)
                        field.SetValue(settings, Convert.ChangeType(mainNode[field.Name].Value, field.FieldType));
                }
            }
            Debug.Log("Settings loaded!");
        }
        return settings;
    }

    public void Save()
    {
        JSONObject mainNode = new JSONObject();
        Type type = GetType();
        FieldInfo[] infos = type.GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(x => x is FieldInfo).OrderBy(x => x.Name).Cast<FieldInfo>().ToArray();
        foreach (FieldInfo info in infos)
            mainNode[info.Name] = info.GetValue(this).ToString();
        using (StreamWriter writer = new StreamWriter(Application.persistentDataPath + "/ChroMapperSettings.json", false))
            writer.Write(mainNode.ToString(2));
        Debug.Log("Settings saved!");
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
