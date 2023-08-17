using System;
using System.Collections;
using System.Collections.Generic;

namespace Aubio
{
    public class AdaptiveWhitening
    {
        private const double defaultRelaxTime = 250; // in seconds, between 22 and 446
        private const double defaultDecay = 0.001f; // -60dB attenuation
        private const double defaultFloor = 1e-4f; // from 1.e-6 to .2

        //private readonly int bufSize;
        private readonly int hopSize;
        private readonly int sampleRate;

        private double relaxTime;
        public double RelaxTime
        {
            get => RelaxTime;
            set
            {
                relaxTime = value;
                rDecay = Math.Pow(defaultDecay, hopSize / (double)sampleRate / relaxTime);
            }
        }
        private double rDecay;
        public double Floor { get; set; }
        private readonly double[] peakValues;

        public AdaptiveWhitening(int bufSize, int hopSize, int sampleRate)
        {
            peakValues = new double[(bufSize / 2) + 1];
            //this.bufSize = bufSize;
            this.hopSize = hopSize;
            this.sampleRate = sampleRate;
            Floor = defaultFloor;

            RelaxTime = defaultRelaxTime;
            Reset();
        }

        public void Do(Polar[] fftGrain)
        {
            var length = Math.Min(fftGrain.Length, peakValues.Length);
            for (var i = 0; i < length; i++)
            {
                var tmp = Math.Max(rDecay * peakValues[i], Floor);
                peakValues[i] = Math.Max(fftGrain[i].Norm, tmp);
                fftGrain[i].Norm /= peakValues[i];
            }
        }

        public void Reset()
        {
            for (var i = 0; i < peakValues.Length; i++) peakValues[i] = Floor;
        }
    }
}
