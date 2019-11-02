using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ImageList", menuName = "ImageList")]
public class ImageList : ScriptableObject {

    public Sprite[] sprites;
    public Sprite DarkSprite;
    [Space]
    [Header("Kiwi dont kill me please")]
    public Sprite DefaultPlatform;
    public Sprite TrianglePlatform;
    public Sprite BigMirrorPlatform;
    public Sprite NicePlatform;
    public Sprite KDAPlatform;
    public Sprite VaporFramePlatform;
    public Sprite BigMirrorV2Platform;
    public Sprite FailsafeBackground;
    
    public Sprite GetRandomSprite() {
        if (Settings.Instance.DarkTheme) return DarkSprite;
        return sprites[Random.Range(0, sprites.Length)];
    }

    public Sprite GetBGSprite(BeatSaberSong song)
    {
        if (Settings.Instance.DarkTheme) return DarkSprite;
        if (song.customData != null)
        {
            if (song.customData && !string.IsNullOrEmpty(song.customData["_customEnvironment"]))
            {
                switch (song.customData["_customEnvironment"].Value)
                {
                    case "Vapor Frame": return VaporFramePlatform;
                    case "Big Mirror V2": return BigMirrorV2Platform;
                }
            }
        }
        switch (song.environmentName)
        {
            case "DefaultEnvironment": return DefaultPlatform;
            case "TriangleEnvironment": return TrianglePlatform;
            case "BigMirrorEnvironment": return BigMirrorPlatform;
            case "NiceEnvironment": return NicePlatform;
            default: return FailsafeBackground; //In case someone does a fucky wucky
        }
    }

}
