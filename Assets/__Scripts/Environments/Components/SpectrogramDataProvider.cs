using UnityEngine;

public class SpectrogramDataProvider : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioLink.AudioLink audioLink;
    public bool HasInitialized;

    private readonly float instantChangeThreshold = 0.1f;

    public const int NumberOfSamples = 64;

    private bool hasData;
    private bool hasProcessedData;

    private readonly float[] samples = new float[NumberOfSamples];
    private readonly float[] processedSamples = new float[NumberOfSamples];

    public float[] Samples
    {
        get
        {
            if (hasData || !(bool)audioSource) return samples;
            audioSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);
            hasData = true;

            return samples;
        }
    }

    public float[] ProcessedSamples
    {
        get
        {
            if (hasProcessedData) return processedSamples;
            ProcessSamples();
            hasProcessedData = true;

            return processedSamples;
        }
    }

    public AudioLink.AudioLink AudioLink
    {
        get => audioLink;
        set
        {
            audioLink = value;
            HasInitialized = audioLink != null;

            if (!HasInitialized) return;
            audioSource = AudioLink.audioSource;
            AudioLink.audioDataToggle = true;
            AudioLink.EnableReadback();
        }
    }

    protected void Awake()
    {
        hasData = false;
        hasProcessedData = false;
    }

    protected void LateUpdate()
    {
        hasData = false;
        hasProcessedData = false;
    }

    private void ProcessSamples()
    {
        var deltaTime = Time.deltaTime;
        var sourceSamples = Samples;
        for (var i = 0; i < sourceSamples.Length; i++)
        {
            var log = Mathf.Log(sourceSamples[i] + 1f) * (i + 1);
            if (processedSamples[i] < log)
            {
                if (log - processedSamples[i] > instantChangeThreshold)
                    processedSamples[i] = log;
                else
                    processedSamples[i] = Mathf.Lerp(processedSamples[i], log, deltaTime * 8f);
            }
            else
                processedSamples[i] = Mathf.Lerp(processedSamples[i], log, deltaTime * 4f);
        }
    }
}
