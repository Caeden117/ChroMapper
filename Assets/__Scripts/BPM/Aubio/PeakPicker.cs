using System;
using System.Linq;

namespace Aubio
{
    public class PeakPicker
    {
        /* peak picking parameters, default values in brackets
         *
         *     [<----post----|--pre-->]
         *  .................|.............
         *  time->           ^now
         */

        /** thresh: offset threshold [0.033 or 0.01] */
        public double Threshold { get; set; }
        /** win_post: median filter window length (causal part) [8] */
        private readonly int winPost;
        /** pre: median filter window (anti-causal part) [post-1] */
        private readonly int winPre;
        /** biquad lowpass filter */
        private readonly Biquad biquad;
        /** original onsets */
        private readonly double[] onsetKeep;
        /** modified onsets */
        private readonly double[] onsetProc;
        /** peak picked window [3] */
        private readonly double[] onsetPeek;
        /** thresholded function */
        public double[] Thresholded { get; private set; }
        /** scratch pad for biquad and median */
        private readonly double[] scratch;

        public PeakPicker()
        {
            Threshold = 0.1; /* 0.0668; 0.33; 0.082; 0.033; */
            winPost = 5;
            winPre = 1;

            scratch = new double[winPost + winPre + 1];
            onsetKeep = new double[winPost + winPre + 1];
            onsetProc = new double[winPost + winPre + 1];
            onsetPeek = new double[3];
            Thresholded = new double[1];

            /* cutoff: low-pass filter with cutoff reduced frequency at 0.34
               generated with octave butter function: [b,a] = butter(2, 0.34);
             */
            biquad = new Biquad(0.15998789, 0.31997577, 0.15998789,
                // FIXME: broken since c9e20ca, revert for now
                //-0.59488894, 0.23484048);
                0.23484048, 0);
        }

        /** modified version for real time, moving mean adaptive threshold this method
         * is slightly more permissive than the offline one, and yelds to an increase
         * of false positives. best  */
        public void Do(double[] onset, double[] output)
        {
            double mean, median;

            /* push new novelty to the end */
            Utils.Push(onsetKeep, onset[0]);
            /* store a copy */
            Array.Copy(onsetKeep, onsetProc, onsetProc.Length);

            /* filter this copy */
            biquad.DoFiltFilt(onsetProc, scratch);

            /* calculate mean and median for onset_proc */
            mean = onsetProc.Average();
            /* copy to scratch and compute its median */
            Array.Copy(onsetProc, scratch, scratch.Length);
            median = Utils.Median(scratch);

            /* shift peek array */
            for (var j = 0; j < 3 - 1; j++)
                onsetPeek[j] = onsetPeek[j + 1];
            /* calculate new tresholded value */
            Thresholded[0] =
                onsetProc[winPost] - median - (mean * Threshold);
            onsetPeek[2] = Thresholded[0];
            if (Utils.PeakPick(onsetPeek, 1))
            {
                output[0] = Utils.QuadracticPeakPos(onsetPeek, 1);
            }
        }
    }
}
