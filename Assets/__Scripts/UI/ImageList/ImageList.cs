using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ImageList", menuName = "ImageList")]
public class ImageList : ScriptableObject
{
    [FormerlySerializedAs("sprites")] public Sprite[] Sprites;
    public Sprite DarkSprite;

    [Space] [Header("Kiwi dont kill me please")]
    public Sprite DefaultPlatform;

    public Sprite TrianglePlatform;
    public Sprite BigMirrorPlatform;
    public Sprite NicePlatform;
    [FormerlySerializedAs("KDAPlatform")] public Sprite KdaPlatform;
    public Sprite VaporFramePlatform;
    public Sprite BigMirrorV2Platform;
    public Sprite FailsafeBackground;

    public Sprite GetRandomSprite() =>
        Settings.Instance.DarkTheme ? DarkSprite : Sprites[Random.Range(0, Sprites.Length)];

    public Sprite GetBgSprite(BeatSaberSong song)
    {
        if (Settings.Instance.DarkTheme) return DarkSprite;
        if (song.CustomData != null)
        {
            if (song.CustomData && !string.IsNullOrEmpty(song.CustomData["_customEnvironment"]))
            {
                switch (song.CustomData["_customEnvironment"].Value)
                {
                    case "Vapor Frame": return VaporFramePlatform;
                    case "Big Mirror V2": return BigMirrorV2Platform;
                }
            }
        }

        return song.EnvironmentName switch
        {
            "DefaultEnvironment" => DefaultPlatform,
            "TriangleEnvironment" => TrianglePlatform,
            "BigMirrorEnvironment" => BigMirrorPlatform,
            "NiceEnvironment" => NicePlatform,
            _ => FailsafeBackground,//In case someone does a fucky wucky
        };
    }
}
