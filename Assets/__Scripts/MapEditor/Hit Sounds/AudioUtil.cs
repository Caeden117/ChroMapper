using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioUtil : MonoBehaviour
{

    private static AudioUtil _instance;

    /// <summary>
    /// Returns the singleton AudioUtil MonoBehaviour.
    /// Will create a new one if it does not exist.
    /// </summary>
    private List<AudioSource> oneShotPool = new List<AudioSource>();

    private AudioSource AvailableOneShot
    {
        get
        {
            for (int i = 0; i < oneShotPool.Count; i++)
            {
                if (oneShotPool[i].isPlaying) continue;
                return oneShotPool[i];
            }
            AudioSource newOneShot = gameObject.AddComponent<AudioSource>();
            MakeSourceNonDimensional(newOneShot, false);
            oneShotPool.Add(newOneShot);
            return newOneShot;
        }
    }

    AudioSource ambianceSource;

    private Dictionary<string, AudioClip> memorizedClips = new Dictionary<string, AudioClip>();

    void Start()
    {
        _instance = this;
        ambianceSource = gameObject.AddComponent<AudioSource>();
        MakeSourceNonDimensional(ambianceSource, true);

        AudioSource mainOneShot = AvailableOneShot;

        //foreach (AudioSource a in GetComponents<AudioSource>()) MakeSourceNonDimensional(a);
    }

    /// <summary>
    /// Plays a sound in the Audio folder once
    /// </summary>
    /// <param name="filenameWithExtension">The file name with extension, found in the Audio folder</param>
    /// <param name="volume">Optional volume multiplier</param>
    /// <param name="pitch">Optional pitch multiplier</param>
    /// <param name="delay">Optional time before sound is played</param>
    /// <returns>The Unity AudioSource used to play the sound</returns>
    public AudioSource PlayOneShotSound(AudioClip clip, float volume = 1f, float pitch = 1f, float delay = 0f)
    {
        AudioSource oneShotSource = AvailableOneShot;
        PlayOneShotSound(clip, oneShotSource, volume, pitch, delay);
        return oneShotSource;
    }

    /// <summary>
    /// Plays a sound in the Audio folder once, with a specific AudioSource
    /// </summary>
    /// <param name="filenameWithExtension">The file name with extension, found in the Audio folder</param>
    /// <param name="oneShotSource">The AudioSource to play the file through</param>
    /// <param name="volume">Optional volume multiplier</param>
    /// <param name="pitch">Optional pitch multiplier</param>
    /// <param name="delay">Optional time before sound is played</param>
    public void PlayOneShotSound(AudioClip clip, AudioSource oneShotSource, float volume = 1f, float pitch = 1f, float delay = 0f)
    {
        oneShotSource.volume = volume;
        oneShotSource.pitch = pitch;
        oneShotSource.clip = clip;
        oneShotSource.PlayScheduled(AudioSettings.dspTime + delay);
    }

    public void StopOneShot()
    {
        foreach (var source in oneShotPool)
        {
            source.Stop();
        }
    }

    /// <summary>
    /// Stops the ambient AudioSource from playing.
    /// </summary>
    public void StopAmbianceSound()
    {
        ambianceSource.Stop();
    }

    /// <summary>
    /// Makes sounds created by the given AudioSource a global, non-directional source.
    /// </summary>
    /// <param name="source">The audio source to flatten</param>
    /// <param name="loop">Whether the audio source should loop or not.</param>
    public static void MakeSourceNonDimensional(AudioSource source, bool loop)
    {
        source.loop = loop;
        source.bypassEffects = true;
        source.bypassListenerEffects = true;
        source.bypassReverbZones = true;
        source.spatialBlend = 0;
        source.spatialize = false;
        source.velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
    }

    /// <summary>
    /// Makes sounds created by the given AudioSource a global, non-directional source.
    /// </summary>
    /// <param name="source">The audio source to flatten</param>
    public static void MakeSourceNonDimensional(AudioSource source)
    {
        MakeSourceNonDimensional(source, source.loop);
    }

}