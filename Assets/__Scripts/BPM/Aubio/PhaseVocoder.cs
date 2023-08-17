using System;

namespace Aubio
{
    // mattmora: functionality is signficantly slimmed down here, so many of the field are not needed
    // leaving them commented for reference.
    public class PhaseVocoder
    {
        //private readonly uint windowSize;       /** grain length */
        private readonly int hopSize;       /** overlap step */
        private readonly DSPLib.FFT fft;  /** fft object */
        private readonly double[] data;      /** current input grain, [win_s] frames */
        private readonly double[] dataold;   /** memory of past grain, [win_s-hop_s] frames */
        //private readonly double[] synth;     /** current output grain, [win_s] frames */
        //private readonly double[] synthold;  /** memory of past grain, [win_s-hop_s] frames */
        private readonly double[] w;         /** grain window [win_s] */
        //private readonly uint start;       /** where to start additive synthesis */
        private readonly int end;         /** where to end it */
        //private readonly double scale;       /** scaling factor for synthesis */
        //private readonly uint end_datasize;  /** size of memory to end */
        //private readonly uint hop_datasize;  /** size of memory to hop_s */

        public PhaseVocoder(int windowSize, int hopSize)
        {
            fft = new DSPLib.FFT();
            fft.Initialize((uint)windowSize);

            /* remember old */
            data = new double[windowSize];
            //synth = new double[windowSize];

            /* new input output */
            if (windowSize > hopSize)
            {
                dataold = new double[windowSize - hopSize];
                //synthold = new double[windowSize - hopSize];
            }
            else
            {
                dataold = new double[1];
                //synthold = new double[1];
            }
            w = DSPLib.DSP.Window.Coefficients(DSPLib.DSP.Window.Type.Hann, (uint)windowSize);

            //this.windowSize = windowSize;
            this.hopSize = hopSize;

            /* more than 50% overlap, overlap anyway */
            //if (windowSize < 2 * hopSize) start = 0;
            /* less than 50% overlap, reset latest grain trail */
            //else start = windowSize - hopSize - hopSize;

            if (windowSize > hopSize) end = windowSize - hopSize;
            else end = 0;

            //end_datasize = end * sizeof(double);
            //hop_datasize = hopSize * sizeof(double);

            // for reconstruction with 75% overlap
            //if (windowSize == hopSize * 4)
            //{
            //    scale = 2.0/ 3.0;
            //}
            //else if (windowSize == hopSize * 8)
            //{
            //    scale = 1.0/ 3.0;
            //}
            //else if (windowSize == hopSize * 2)
            //{
            //    scale = 1.0;
            //}
            //else
            //{
            //    scale = .5;
            //}
        }

        public Polar[] Do(double[] dataNew)
        {
            /* slide  */
            SwapBuffers(dataNew);
            /* windowing */
            Utils.MultiplyInPlace(data, w);
            /* shift */
            Utils.Shift(data);
            /* calculate fft */
            return Utils.ToPolar(fft.Execute(data));
        }

        // DSPLib's FFT does not have a reverse method and I don't want to port aubio's fft, so not implementing this
        // It's not needed anyway
        //public void Reverse(Complex[] fftGrain, double synthNew)
        //{
        //    /* calculate rfft */
        //    aubio_fft_rdo(pv->fft, fftgrain, pv->synth);
        //    /* unshift */
        //    fvec_ishift(pv->synth);
        //    /* windowing */
        //    // if overlap = 50%, do not apply window (identity)
        //    if (hopSize * 2 < windowSize)
        //    {
        //        Utils.MultiplyInPlace(data, w);
        //    }
        //    /* additive synthesis */
        //    aubio_pvoc_addsynth(pv, synthnew);
        //}

        /** returns data and dataold slided by hop_s */
        private void SwapBuffers(double[] dataNew)
        {
            Array.Copy(dataold, 0, data, 0, end);
            Array.Copy(dataNew, 0, data, end, hopSize);
            Array.Copy(data, hopSize, dataold, 0, end);
        }

        /** do additive synthesis from 'old' and 'cur' */
        //private void AddSynth(float[] synthNew)
        //{
        //    uint_t i;
        //    /* some convenience pointers */
        //    smpl_t* synth = pv->synth->data;
        //    smpl_t* synthold = pv->synthold->data;
        //    smpl_t* synthnew = synth_new->data;

        //    /* put new result in synthnew */
        //    for (i = 0; i < pv->hop_s; i++)
        //        synthnew[i] = synth[i] * pv->scale;

        //    /* no overlap, nothing else to do */
        //    if (pv->end == 0) return;

        //    /* add new synth to old one */
        //    for (i = 0; i < pv->hop_s; i++)
        //        synthnew[i] += synthold[i];

        //    /* shift synthold */
        //    for (i = 0; i < pv->start; i++)
        //        synthold[i] = synthold[i + pv->hop_s];

        //    /* erase last frame in synthold */
        //    for (i = pv->start; i < pv->end; i++)
        //        synthold[i] = 0.;

        //    /* additive synth */
        //    for (i = 0; i < pv->end; i++)
        //        synthold[i] += synth[i + pv->hop_s] * pv->scale;
        //}
    }
}

