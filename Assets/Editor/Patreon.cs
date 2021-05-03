using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class Patreon : EditorWindow
{
    [MenuItem("Edit/Patreon")]
    public static void ShowWindow()
    {
        GetWindow(typeof(Patreon));
    }

    private string clientSecret = "";
    private string filteredPatrons = "";
    private IEnumerable<string> filteredPatreonList = Enumerable.Empty<string>();
    private PatreonSupporters modifyingList = null;
    private bool clearLists = false;

    private string chroMapperTierID = "";
    private Dictionary<string, JSONNode> userIDToAttributes = new Dictionary<string, JSONNode>();

    private int? numberOfSupporters = null;
    private int supportersProcessed = 0;

    private bool currentSupporterIsCMPatron = false;
    private string currentSupporterDiscordUsername = "";

    private void OnGUI()
    {
        maxSize = new Vector2(250f, 150f);
        minSize = maxSize;
        titleContent = new GUIContent("Pull Patreon Supporters");

        GUILayout.Label("Patreon Client Access Token:");
        clientSecret = GUILayout.TextField(clientSecret);
        GUILayout.Label("Filtered Patreon Users");
        filteredPatrons = GUILayout.TextField(filteredPatrons);
        filteredPatreonList = filteredPatrons.Split(',').Select(x => x.Trim());
        clearLists = GUILayout.Toggle(clearLists, "Clear Existing List");
        GUILayout.Label("Object to Modify");
        modifyingList = (PatreonSupporters)EditorGUILayout.ObjectField(modifyingList, typeof(PatreonSupporters), false);

        if (GUILayout.Button("Pull from Patreon"))
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(Pull());
        }
    }

    private IEnumerator Pull()
    {
        var client = CreateRequest("https://www.patreon.com/api/oauth2/api/current_user/campaigns", true);
        
        if (clearLists)
        {
            modifyingList.HighTierPatrons.Clear();
            modifyingList.RegularPatrons.Clear();
        }

        yield return client.SendWebRequest();

        JSONNode json = JSON.Parse(client.downloadHandler.text);
        string campaignID = json["included"][0]["relationships"]["campaign"]["data"]["id"]; // we're going on a trip, down some JSON that's a rip

        string req = $"https://www.patreon.com/api/oauth2/v2/campaigns/{campaignID}/members?include=currently_entitled_tiers,user&fields%5Buser%5D=social_connections,first_name&fields%5Btier%5D=title";
        yield return EditorCoroutineUtility.StartCoroutineOwnerless(GetAllSupporters(req));

        EditorUtility.ClearProgressBar();
    }

    private IEnumerator GetAllSupporters(string req)
    {
        var client = CreateRequest(req, true);

        yield return client.SendWebRequest();

        JSONNode json = JSON.Parse(client.downloadHandler.text);

        if (!numberOfSupporters.HasValue)
        {
            numberOfSupporters = json["meta"]["pagination"]["total"];
        }
        foreach (JSONNode include in json["included"])
        {
            if (string.IsNullOrEmpty(chroMapperTierID) && include["type"] == "tier" && include["attributes"]["title"] == "ChroMapper")
            {
                chroMapperTierID = include["id"];
            }
            else if (include["type"] == "user")
            {
                userIDToAttributes.Add(include["id"], include["attributes"]);
            }
        }
        foreach (JSONNode member in json["data"])
        {
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(GetUserInformation(member));
            supportersProcessed++;

            if (currentSupporterDiscordUsername == "ERR") continue;

            modifyingList.AddSupporter(currentSupporterDiscordUsername, currentSupporterIsCMPatron);
            yield return new EditorWaitForSeconds(3);
            EditorUtility.DisplayProgressBar("Retrieving Patreon Supporters", "Retrieving information on Patreon supporters", (float)supportersProcessed / numberOfSupporters.Value);
        }

        if (json.HasKey("links") && json["links"].HasKey("next"))
        {
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(GetAllSupporters(json["links"]["next"])); // oh yeah we're going recursive on this bitch
        }
    }

    private IEnumerator GetUserInformation(JSONNode dataObj)
    {
        currentSupporterIsCMPatron = dataObj["relationships"]["currently_entitled_tiers"]["data"].Linq.Any(x => x.Value["id"] == chroMapperTierID);

        string userID = dataObj["relationships"]["user"]["data"]["id"];
        JSONNode userAttributes = userIDToAttributes[userID];

        if (userAttributes.HasKey("social_connections") && userAttributes["social_connections"].HasKey("discord") && !userAttributes["social_connections"]["discord"].IsNull)
        {
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(ContactAuros(userAttributes["social_connections"]["discord"]["user_id"]));
        }
        else
        {
            currentSupporterDiscordUsername = userAttributes["first_name"];
        }

        if (filteredPatreonList.Contains(currentSupporterDiscordUsername)) currentSupporterDiscordUsername = "ERR";
    }

    private IEnumerator ContactAuros(string id)
    {
        var client = CreateRequest($"https://suit.auros.dev/api/discord/user/{id}");
        yield return client.SendWebRequest();
        try
        {
            JSONNode json = JSON.Parse(client.downloadHandler.text);
            currentSupporterDiscordUsername = json["username"];
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            currentSupporterDiscordUsername = "ERR";
        }
    }

    private UnityWebRequest CreateRequest(string uri, bool authorized = false)
    {
        UnityWebRequest client = UnityWebRequest.Get(uri);
        client.SetRequestHeader("User-Agent", $"ChroMapper-Patreon-Member-Retrieval");

        if (authorized)
        {
            client.SetRequestHeader("Authorization", $"Bearer {clientSecret}");
        }

        return client;
    }
}
