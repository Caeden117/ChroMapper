using UnityEngine;

[CreateAssetMenu(fileName = "Bongo Cat", menuName = "Bongo Cat Preset")]
public class BongoCatPreset : ScriptableObject
{
    public Sprite LeftDownRightDown;
    public Sprite LeftDownRightUp;
    public Sprite LeftUpRightDown;
    public Sprite LeftUpRightUp;

    public float YOffset;

    public Vector2 Scale;
}
