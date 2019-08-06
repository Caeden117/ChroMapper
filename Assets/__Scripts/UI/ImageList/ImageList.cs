using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ImageList", menuName = "ImageList")]
public class ImageList : ScriptableObject {

    public Sprite[] sprites;
    [Space]
    [Header("Kiwi dont kill me please")]
    public Sprite DefaultPlatform;
    public Sprite TrianglePlatform;
    public Sprite BigMirrorPlatform;
    public Sprite NicePlatform;
    public Sprite KDAPlatform;
    public Sprite FailsafeBackground;
    
    public Sprite GetRandomSprite() {
        return sprites[Random.Range(0, sprites.Length)];
    }

    public Sprite GetBGSprite(BeatSaberSong song)
    {
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
