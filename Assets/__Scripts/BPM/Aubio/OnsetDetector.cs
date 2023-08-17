using System;
using System.Diagnostics;

namespace Aubio
{
    /// <summary>
    /// Onset detection
    /// </summary>
    public class OnsetDetector
    {
        private readonly PhaseVocoder pv;        /**< phase vocoder */
        private readonly SpectralDescription od; /**< spectral descriptor */
        private readonly PeakPicker pp;          /**< peak picker */
        private Polar[] fftGrain;       /**< phase vocoder output */
        private readonly double[] desc;          /**< spectral description */
        public double Silence { get; set; } /**< silence threhsold */
        public int MinIOI { get; set; } /**< minimum inter onset interval */
        public double MinIOIS { get => MinIOI / (double)sampleRate; set => MinIOI = (int)Math.Round(value * sampleRate); }
        public double MinIOIMS { get => MinIOIS * 1000.0; set => MinIOIS = value / 1000.0; }

        public int Delay { get; set; } /**< constant delay, in samples, removed from detected onset times */
        private readonly int sampleRate;        /**< sampling rate of the input signal */
        private readonly int hopSize;          /**< number of samples between two runs */

        private int totalFrames;      /**< total number of frames processed since the beginning */
        private int lastOnset; /**< last detected onset location, in frames */
        public int Last { get => lastOnset - Delay; private set { } } /**< last detected onset location, in frames */
        public double LastS { get => Last / (double)sampleRate; private set { } }
        public double LastMS { get => LastS * 1000.0; private set { } }

        public double Threshold { get => pp.Threshold; set => pp.Threshold = value; }
        public double Descriptor { get => desc[0]; private set { } }
        public double ThresholdedDescriptor { get => pp.Thresholded[0]; private set { } }

        private bool applyCompression;
        private double lambdaCompression;
        public double Compression
        {
            get => applyCompression ? lambdaCompression : 0.0;
            set
            {
                Debug.Assert(value > 0);
                lambdaCompression = value;
                applyCompression = lambdaCompression > 0.0;
            }
        }

        private readonly AdaptiveWhitening spectralWhitening;
        public bool ApplyWhitening { get; set; }      /**< apply adaptive spectral whitening */

        public OnsetDetector(SpectralDescription.OnsetType type, int bufSize, int hopSize, int sampleRate)
        {
            /* store creation parameters */
            this.sampleRate = sampleRate;
            this.hopSize = hopSize;

            pv = new PhaseVocoder(bufSize, hopSize);
            pp = new PeakPicker();
            od = new SpectralDescription(type, bufSize);
            fftGrain = new Polar[bufSize];
            desc = new double[1];
            spectralWhitening = new AdaptiveWhitening(bufSize, hopSize, sampleRate);

            /* initialize internal variables */
            SetDefaultParameters(type);

            Reset();
        }

        public void Reset()
        {
            lastOnset = 0;
            totalFrames = 0;
        }

        private void SetDefaultParameters(SpectralDescription.OnsetType type)
        {
            /* set some default parameter */
            Threshold = 0.3;
            Delay = (int)(4.3 * hopSize);
            MinIOIMS = 50;
            Silence = -70;
            // disable spectral whitening
            ApplyWhitening = false;
            // disable logarithmic magnitude
            Compression = 0.0;

            /* method specific optimisations */
            //if (strcmp(onset_mode, "energy") == 0)
            //{
            //}
            //else 
            if (type == SpectralDescription.OnsetType.HFC)
            {
                Threshold = 0.058;
                Compression = 1;
            }
            else if (type == SpectralDescription.OnsetType.ComplexDomain) 
            {
                Delay = (int)(4.6 * hopSize);
                Threshold = 0.15;
                ApplyWhitening = true;
                Compression = 1.0;
            }
            //else if (strcmp(onset_mode, "phase") == 0)
            //{
            //    o->apply_compression = 0;
            //    aubio_onset_set_awhitening(o, 0);
            //}
            //else if (strcmp(onset_mode, "wphase") == 0)
            //{
            //    // use defaults for now
            //}
            //else if (strcmp(onset_mode, "mkl") == 0)
            //{
            //    aubio_onset_set_threshold(o, 0.05);
            //    aubio_onset_set_awhitening(o, 1);
            //    aubio_onset_set_compression(o, 0.02);
            //}
            else if (type == SpectralDescription.OnsetType.KL)
            {
                Threshold = 0.35;
                ApplyWhitening = true;
                Compression = 0.02;
            }
            else if (type == SpectralDescription.OnsetType.SpectralFlux)
            {
                Threshold = 0.18;
                ApplyWhitening = true;
                spectralWhitening.RelaxTime = 100;
                spectralWhitening.Floor = 1.0;
                Compression = 10.0;
            }
            //else if (strcmp(onset_mode, "specdiff") == 0)
            //{
            //}
            //else if (strcmp(onset_mode, "old_default") == 0)
            //{
            //    // used to reproduce results obtained with the previous version
            //    aubio_onset_set_threshold(o, 0.3);
            //    aubio_onset_set_minioi_ms(o, 20.);
            //    aubio_onset_set_compression(o, 0.);
            //}
        }

        /* execute onset detection function on iput buffer */
        public void Do(double[] input, double[] onset)
        {
            double isOnset;
            fftGrain = pv.Do(input);

            if (ApplyWhitening)
            {
                spectralWhitening.Do(fftGrain);
            }
            if (applyCompression)
            {
                for (var i = 0; i < fftGrain.Length; i++) fftGrain[i].LogMag(lambdaCompression);
            }
            od.Do(fftGrain, desc);
            pp.Do(desc, onset);
            isOnset = onset[0];
            if (isOnset > 0.0)
            {
                if (Utils.SilenceDetection(input, Silence))
                {
                    //AUBIO_DBG ("silent onset, not marking as onset\n");
                    isOnset = 0;
                }
                else
                {
                    // we have an onset
                    var new_onset = totalFrames + (int)Math.Round(isOnset * hopSize);
                    // check if last onset time was more than minioi ago
                    if (lastOnset + MinIOI < new_onset)
                    {
                        // start of file: make sure (new_onset - delay) >= 0
                        if (lastOnset > 0 && Delay > new_onset)
                        {
                            isOnset = 0;
                        }
                        else
                        {
                            //AUBIO_DBG ("accepted detection, marking as onset\n");
                            lastOnset = Math.Max(Delay, new_onset);
                        }
                    }
                    else
                    {
                        //AUBIO_DBG ("doubled onset, not marking as onset\n");
                        isOnset = 0;
                    }
                }
            }
            else
            {
                // we are at the beginning of the file
                if (totalFrames <= Delay)
                {
                    // and we don't find silence
                    if (!Utils.SilenceDetection(input, Silence))
                    {
                        var newOnset = totalFrames;
                        if (totalFrames == 0 || lastOnset + MinIOI < newOnset)
                        {
                            isOnset = Delay / hopSize;
                            lastOnset = totalFrames + Delay;
                        }
                    }
                }
            }
            onset[0] = isOnset;
            totalFrames += hopSize;
            return;
        }
    }
}
