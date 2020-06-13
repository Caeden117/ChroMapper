using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PreviewSong : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private TMP_InputField previewStartTime;
    [SerializeField] private TMP_InputField previewDuration;

    [SerializeField] private Image image;
    [SerializeField] private Sprite startSprite;
    [SerializeField] private Sprite stopSprite;

    float length = 10f;
    double startTime = 0;
    bool playing = true;

    public void Start()
    {
        // Trigger stop action which resets the UI
        PlayClip();
    }

    public void PlayClip()
    {
        if (playing)
        {
            progressBar.fillAmount = 0f;
            image.sprite = startSprite;
            audioSource.Stop();
            playing = false;
            return;
        }

        if (float.TryParse(previewDuration.text, out length))
        {
            if (float.TryParse(previewStartTime.text, out float start))
            {
                playing = true;
                startTime = AudioSettings.dspTime;
                audioSource.time = start;
                audioSource.Play();
                image.sprite = stopSprite;
            }
        }
    }

    public void Update()
    {
        if (!playing)
            return;

        float time = (float) (AudioSettings.dspTime - startTime);
        float timeRemaining = length - time;
        if (timeRemaining <= 0)
        {
            PlayClip();
        }
        else if (timeRemaining < 1)
        {
            audioSource.volume = timeRemaining;
        }
        else if (time < 0.25)
        {
            audioSource.volume = time * 4;
        }
        else
        {
            audioSource.volume = 1;
        }

        float position = time > length ? 0 : time / length;
        progressBar.fillAmount = position;
    }

}