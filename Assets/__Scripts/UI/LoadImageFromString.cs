using System.Collections.Generic;
using UnityEngine;

//Overwrites a sprite in a SpriteRenderer with bytes from a text asset.
//May or may not be used for a little... easter egg.... nah, I wouldn't do that...
public class LoadImageFromString : MonoBehaviour
{
    [SerializeField] private TextAsset bytes;
    [SerializeField] private SpriteRenderer spriteImage;

    private void Start()
    {
        var allBytes = new List<byte>();
        foreach (var byteString in bytes.text.Split(','))
        {
            var decompiledByte = byte.Parse(byteString);
            allBytes.Add(decompiledByte);
        }

        var tex = new Texture2D(2, 2);
        if (tex.LoadImage(allBytes.ToArray()))
            spriteImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2f);
    }
}
