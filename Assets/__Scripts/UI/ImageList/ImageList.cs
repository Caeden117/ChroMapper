using Beatmap.Info;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ImageList", menuName = "ImageList")]
public class ImageList : ScriptableObject
{
    [FormerlySerializedAs("sprites")] public Sprite[] Sprites;
    public Sprite DarkSprite;

    [Space]
    [Header("Kiwi dont kill me please")]
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

    public Sprite GetBgSprite(BaseInfo mapInfo)
    {
        if (Settings.Instance.DarkTheme) return DarkSprite;
        if (mapInfo.CustomData != null)
        {
            if (mapInfo.CustomData && !string.IsNullOrEmpty(mapInfo.CustomData["_customEnvironment"]))
            {
                switch (mapInfo.CustomData["_customEnvironment"].Value)
                {
                    case "Vapor Frame": return VaporFramePlatform;
                    case "Big Mirror V2": return BigMirrorV2Platform;
                }
            }
        }

        return mapInfo.EnvironmentName switch
        {
            "DefaultEnvironment" => DefaultPlatform,
            "TriangleEnvironment" => TrianglePlatform,
            "BigMirrorEnvironment" => BigMirrorPlatform,
            "NiceEnvironment" => NicePlatform,
            _ => FailsafeBackground,//In case someone does a fucky wucky
        };
    }
}
