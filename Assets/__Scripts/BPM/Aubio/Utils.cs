using System;
using System.Diagnostics;
using System.Numerics;
using UnityEngine.UIElements;

namespace Aubio
{
    // Based on aubio mathutils.c, does not include things that exist in DSPLib
    public static class Utils
    {
        private const double verySmallNumber = 2e-42;

        public static bool IsDenormal(double f) => f < verySmallNumber;

        public static double KillDenormal(double f) => IsDenormal(f) ? 0.0 : f;

        // Modified from DSP to be slightly more efficient when modifying a is ok
        /// <summary>
        /// a[] = a[] * b[]
        /// </summary>
        public static void MultiplyInPlace(double[] a, double[] b)
        {
            Debug.Assert(a.Length == b.Length, "Length of arrays a[] and b[] must match.");

            for (var i = 0; i < a.Length; i++)
                a[i] = a[i] * b[i];
        }

        /// <summary>
        /// Swap the halves of a[].
        /// This operation, known as 'fftshift' in the Matlab Signal Processing Toolbox,
        /// can be used before computing the FFT to simplify the phase relationship of the
        /// resulting spectrum.See Amalia de Götzen's paper referred to above.
        /// </summary>
        public static void Shift(double[] a)
        {
            int half = a.Length / 2, start = half, j;
            // if length is odd, middle element is moved to the end
            if (2 * half < a.Length) start++;

            double tmp;
            for (j = 0; j < half; j++)
            {
                tmp = a[j];
                a[j] = a[j + start];
                a[j + start] = tmp;
            }

            if (start != half)
            {
                for (j = 0; j < half; j++)
                {
                    tmp = a[j + start - 1];
                    a[j + start - 1] = a[j + start];
                    a[j + start] = tmp;
                }
            }
        }

        /// <summary>
        /// Push to the end of a[], removing the first elementing and shifting the rest
        /// </summary>
        public static void Push(double[] a, double newElement)
        {
            for (var i = 0; i < a.Length - 1; i++)
            {
                a[i] = a[i + 1];
            }
            a[a.Length - 1] = newElement;
        }

        // Combination of DSPLib.DSP.ConvertComplex.ToMagnitude and ToPhaseRadians
        // Polar corresponds to aubio's complex number representation
        /// <summary>
        /// Convert Complex DFT/FFT result Polar coordinates
        /// </summary>
        /// <param name="rawFFT"> Complex[] input array"></param>
        /// <returns>Polar[] Phase (Radians)</returns>
        public static Polar[] ToPolar(Complex[] rawFFT)
        {
            var np = rawFFT.Length;
            var polar = new Polar[np];
            for (var i = 0; i < np; i++)
            {
                polar[i] = new Polar(rawFFT[i].Magnitude, rawFFT[i].Phase);
            }

            return polar;
        }


        public static double Median(double[] arr)
        {
            var n = arr.Length;
            int low, high;
            int median;
            int middle, ll, hh;
            double tmp;

            low = 0; high = n - 1; median = (low + high) / 2;
            for (; ; )
            {
                if (high <= low) /* One element only */
                    return arr[median];

                if (high == low + 1)
                {  /* Two elements only */
                    if (arr[low] > arr[high])
                    {
                        tmp = arr[low];
                        arr[low] = arr[high];
                        arr[high] = tmp;
                    }
                    return arr[median];
                }

                /* Find median of low, middle and high items; swap into position low */
                middle = (low + high) / 2;
                if (arr[middle] > arr[high])
                {
                    tmp = arr[middle];
                    arr[middle] = arr[high];
                    arr[high] = tmp;
                }
                if (arr[low] > arr[high])
                {
                    tmp = arr[low];
                    arr[low] = arr[high];
                    arr[high] = tmp;
                }
                if (arr[middle] > arr[low])
                {
                    tmp = arr[middle];
                    arr[middle] = arr[low];
                    arr[low] = tmp;
                }

                /* Swap low item (now in position middle) into position (low+1) */
                tmp = arr[middle];
                arr[middle] = arr[low + 1];
                arr[low + 1] = tmp;

                /* Nibble from each end towards middle, swapping items when stuck */
                ll = low + 1;
                hh = high;
                for (; ; )
                {
                    do ll++; while (arr[low] > arr[ll]);
                    do hh--; while (arr[hh] > arr[low]);

                    if (hh < ll)
                        break;

                    tmp = arr[ll];
                    arr[ll] = arr[hh];
                    arr[hh] = tmp;
                }

                /* Swap middle item (in position low) back into correct position */
                tmp = arr[low];
                arr[low] = arr[hh];
                arr[hh] = tmp;

                /* Re-set active partition */
                if (hh <= median)
                    low = ll;
                if (hh >= median)
                    high = hh - 1;
            }
        }

        public static bool PeakPick(in double[] onset, int pos)
        {
            return onset[pos] > onset[pos - 1]
                && onset[pos] > onset[pos + 1]
                && onset[pos] > 0.0;
        }

        /** finds exact peak index by quadratic interpolation
          See [Quadratic Interpolation of Spectral
          Peaks](https://ccrma.stanford.edu/~jos/sasp/Quadratic_Peak_Interpolation.html),
          by Julius O. Smith III

          \f$ p_{frac} = \frac{1}{2} \frac {x[p-1] - x[p+1]} {x[p-1] - 2 x[p] + x[p+1]} \in [ -.5, .5] \f$

          \param x vector to get the interpolated peak position from
          \param p index of the peak in vector `x`
          \return \f$ p + p_{frac} \f$ exact peak position of interpolated maximum or minimum
        */
        public static double QuadracticPeakPos(in double[] x, int pos)
        {
            double s0, s1, s2; int x0, x2;
            double half = .5, two = 2.0;
            if (pos == 0 || pos == x.Length - 1) return pos;
            x0 = (pos < 1) ? pos : pos - 1;
            x2 = (pos + 1 < x.Length) ? pos + 1 : pos;
            if (x0 == pos) return (x[pos] <= x[x2]) ? pos : x2;
            if (x2 == pos) return (x[pos] <= x[x0]) ? pos : x0;
            s0 = x[x0];
            s1 = x[pos];
            s2 = x[x2];
            return pos + (half * (s0 - s2) / (s0 - (two * s1) + s2));
        }

        public static double LevelLinear(double[] f)
        {
            var energy = 0.0;
            for (var j = 0; j < f.Length; j++)
            {
                energy += f[j] * f[j];
            }
            return energy / f.Length;
        }

        public static double DBSoundPressureLevel(double[] o) => 10.0 * Math.Log10(LevelLinear(o));

        public static bool SilenceDetection(double[] o, double threshold) => DBSoundPressureLevel(o) < threshold;
    }
}
