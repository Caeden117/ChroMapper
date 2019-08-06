using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundList", menuName = "SoundList")]
public class SoundList : ScriptableObject
{
    public AudioClip[] LongClips;

    public AudioClip[] ShortClips;

    public AudioClip GetRandomClip(bool IsShortCut)
    {
        if (!IsShortCut)
            return LongClips[Random.Range(0, LongClips.Length)];
        else
            return ShortClips[Random.Range(0, ShortClips.Length)];
    }
}
