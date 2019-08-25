using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;
using System.IO;

public class ColourHistory : MonoBehaviour {
    private static ColourHistory Instance;
    private static List<Transform> ChildTransforms = new List<Transform>();
    internal static List<ColourSnapshot> RecentlyUsedColours = new List<ColourSnapshot>();
    private static int recentColours = 0;

    public static void Save()
    {
        JSONObject obj = new JSONObject();
        JSONArray colors = new JSONArray();
        foreach (ColourSnapshot snapshot in RecentlyUsedColours) colors.Add(snapshot.ToNode());
        obj.Add("colors", colors);
        using (StreamWriter writer = new StreamWriter(Application.persistentDataPath + "/ChromaColors.json", false))
            writer.Write(obj.ToString());
    }

    public static void Load()
    {
        if (!File.Exists(Path.Combine(Application.persistentDataPath, "/ChromaColors.json")))
        {
            Debug.Log("Chroma Colors file doesn't exist! Skipping loading...");
            return;
        }
        try
        {
            using (StreamReader reader = new StreamReader(Application.persistentDataPath + "/ChromaColors.json"))
            {
                JSONNode mainNode = JSON.Parse(reader.ReadToEnd());
                RecentlyUsedColours.Clear();
                foreach (JSONNode n in mainNode["colors"].AsArray)
                    RecentlyUsedColours.Add(new ColourSnapshot(n));
            }
            Debug.Log("Loaded " + RecentlyUsedColours.Count + " colors!");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    void Start()
    {
        ChildTransforms.Clear();
        for (int i = 0; i < transform.childCount; i++)
            ChildTransforms.Add(transform.GetChild(i));
        foreach (Transform child in ChildTransforms) child.GetComponent<Button>().onClick.AddListener(() => ClickColourHistoryButton(child));
        Instance = this;
        RefreshList();
    }

    void OnDestroy()
    {
        foreach (Transform child in ChildTransforms) child.GetComponent<Button>().onClick.RemoveAllListeners();
    }

    void ClickColourHistoryButton(Transform identifier)
    {
        ColourHistoryContainer container = identifier.GetComponent<ColourHistoryContainer>();
        ColourSnapshot snapshot = RecentlyUsedColours.Where((ColourSnapshot x) => x.color == container.color && x.timeCreated == container.timeCreated).FirstOrDefault();
        if (snapshot != null)
        {
            RecentlyUsedColours.Remove(snapshot);
            EventPreview.QueuedChromaColor = ColourManager.ColourToInt(snapshot.color);
            identifier.GetComponent<Image>().color = new Color(54f/255f, 54f/255f, 54f/255f);
            recentColours--;
        }
        RefreshList();
    }

    public static void AddColour(Color newColour)
    {
        if (RecentlyUsedColours.Count > 0)
            if (RecentlyUsedColours.First().color == newColour) return;
        ColourSnapshot snapshot = new ColourSnapshot(newColour);
        RecentlyUsedColours.Add(snapshot);
        recentColours++;
        RefreshList();
    }

    private static void RefreshList()
    {
        RecentlyUsedColours = RecentlyUsedColours.OrderByDescending((ColourSnapshot x) => x.timeCreated).ToList();
        if (RecentlyUsedColours.Count >= 20)
        {
            while (RecentlyUsedColours.Count > 20) RecentlyUsedColours.Remove(RecentlyUsedColours.Last());
        }
        foreach (Transform child in ChildTransforms)
            child.GetComponent<Image>().color = new Color(54f/255f, 54f/255f, 54/255f);
        foreach (ColourSnapshot snapshot in RecentlyUsedColours)
        {
            Transform child = Instance.transform.Find("Recent Colour " + (RecentlyUsedColours.IndexOf(snapshot) + 1));
            child.GetComponent<Image>().color = snapshot.color;
            child.GetComponent<ColourHistoryContainer>().color = snapshot.color;
            child.GetComponent<ColourHistoryContainer>().timeCreated = snapshot.timeCreated;
            child.GetComponent<Button>().interactable = true;
        }
    }

    public class ColourSnapshot
    {
        public Color color;
        public DateTime timeCreated;

        public ColourSnapshot(Color newColour)
        {
            color = newColour;
            timeCreated = DateTime.Now;
        }

        public ColourSnapshot(JSONNode node)
        {
            color = ColourManager.ColourFromInt(node["color"].AsInt);
            timeCreated = DateTime.Parse(node["time"]);
        }

        public JSONNode ToNode()
        {
            JSONObject obj = new JSONObject();
            obj["color"] = ColourManager.ColourToInt(color);
            obj["time"] = timeCreated.ToString("o");
            return obj;
        }
    }
}
