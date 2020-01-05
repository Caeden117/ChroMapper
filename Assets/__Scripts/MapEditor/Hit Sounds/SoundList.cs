using UnityEngine;

[CreateAssetMenu(fileName = "SoundList", menuName = "SoundList")]
public class SoundList : ScriptableObject
{
    public AudioClip[] LongClips;
    public AudioClip[] ShortClips;

    public AudioClip GetRandomClip(bool IsShortCut)
    {
        
        return !IsShortCut && LongClips.Length>0 ? LongClips[Random.Range(0, LongClips.Length)] : ShortClips[Random.Range(0, ShortClips.Length)];
    }
}
