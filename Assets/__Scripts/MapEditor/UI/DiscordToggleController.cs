using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscordToggleController : MonoBehaviour
{
    private DiscordController discord;

    private void Start()
    {
        discord = PersistentUI.Instance.gameObject.GetComponent<DiscordController>();
    }

    public void UpdateDiscord(bool enabled)
    {
        discord.UpdateDiscord(enabled);
    }
}
