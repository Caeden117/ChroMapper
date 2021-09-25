﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;


// =====[ Revision History ]==========================================
// 17Jun16 - 1.0 - First release - Steve Hageman
// 20Jun16 - 1.01 - Made some variable terms consistent - Steve Hageman
// 16Jul16 - 1.02 - Calculated sign of DFT phase was not consistent with that of the FFT. ABS() of phase was right.
//                  FFT with zero padding did not correctly clean up first runs results.
//                  Added UnwrapPhaseDegrees() and UnwrapPhaseRadians() to Analysis Class.
// 04Jul17 - 1.03 - Added zero or negative check to all Log10 operations.
// 15Oct17 - 1.03.1 - Slight interoperability correction to V1.03, same results, different design pattern.
// 27Aug21 - 1.03.2(?) - Enforced ChroMapper .editorconfig
//


namespace DSPLib
{

    #region =====[ DFT Core Class ]======================================================

    /**
     * Performs a complex DFT w/Optimizations for .NET >= 4.
     *
     * Released under the MIT License
     *
     * DFT Core Functions Copyright (c) 2016 Steven C. Hageman
     *
     * Permission is hereby granted, free of charge, to any person obtaining a copy
     * of this software and associated documentation files (the "Software"), to
     * deal in the Software without restriction, including without limitation the
     * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
     * sell copies of the Software, and to permit persons to whom the Software is
     * furnished to do so, subject to the following conditions:
     *
     * The above copyright notice and this permission notice shall be included in
     * all copies or substantial portions of the Software.
     *
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
     * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
     * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
     * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
     * IN THE SOFTWARE.
     */

    /// <summary>
    /// DFT Base Class
    /// </summary>
    public class DFT
    {
        /// <summary>
        /// DFT Class
        /// </summary>
        public DFT() { }

        #region Properties

        private double mDFTScale;       // DFT ONLY Scale Factor
        private uint mLengthTotal;    // mN + mZp
        private uint mLengthHalf;     // (mN + mZp) / 2

        private double[,] mCosTerm;      // Caching of multiplication terms to save time
        private double[,] mSinTerm;      // on smaller DFT's
        private bool mOutOfMemory;       // True = Caching ran out of memory.


        /// <summary>
        /// Read only Boolean property. True meas the currently defined DFT is using cached memory to speed up calculations.
        /// </summary>
        public bool IsUsingCached
        {
            private set { }
            get => !mOutOfMemory;
        }

        #endregion

        #region Core DFT Routines

        /// <summary>
        /// Pre-Initializes the DFT.
        /// Must call first and this anytime the FFT setup changes.
        /// </summary>
        /// <param name="inputDataLength"></param>
        /// <param name="zeroPaddingLength"></param>
        /// <param name="forceNoCache">True will force the DFT to not use pre-calculated caching.</param>
        public void Initialize(uint inputDataLength, uint zeroPaddingLength = 0, bool forceNoCache = false)
        {
            // Save the sizes for later
            mLengthTotal = inputDataLength + zeroPaddingLength;
            mLengthHalf = (mLengthTotal / 2) + 1;

            // Set the overall scale factor for all the terms
            mDFTScale = Math.Sqrt(2) / (inputDataLength + zeroPaddingLength);                 // Natural DFT Scale Factor                                           // Window Scale Factor
            mDFTScale *= (inputDataLength + zeroPaddingLength) / (double)inputDataLength;   // Account For Zero Padding                           // Zero Padding Scale Factor


            if (forceNoCache)
            {
                // If optional No Cache - just flag that the cache failed
                // then the routines will use the brute force DFT methods.
                mOutOfMemory = true;
                return;
            }

            // Try to make pre-calculated sin/cos arrays. If not enough memory, then
            // use a brute force DFT.
            // Note: pre-calculation speeds the DFT up by about 5X (on a core i7)
            mOutOfMemory = false;
            try
            {
                mCosTerm = new double[mLengthTotal, mLengthTotal];
                mSinTerm = new double[mLengthTotal, mLengthTotal];

                var scaleFactor = 2.0 * Math.PI / mLengthTotal;

                //Parallel.For(0, mLengthHalf, (j) =>
                for (var j = 0; j < mLengthHalf; j++)
                {
                    var a = j * scaleFactor;
                    for (var k = 0; k < mLengthTotal; k++)
                    {
                        mCosTerm[j, k] = Math.Cos(a * k) * mDFTScale;
                        mSinTerm[j, k] = Math.Sin(a * k) * mDFTScale;
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                // Could not allocate enough room for the cache terms
                // So, will use brute force DFT
                mOutOfMemory = true;
            }
        }


        /// <summary>
        /// Execute the DFT.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <returns>Complex[] FFT Result</returns>
        public Complex[] Execute(double[] timeSeries)
        {
            Debug.Assert(timeSeries.Length <= mLengthTotal, "The input timeSeries length was greater than the total number of points that was initialized. DFT.Exectue()");

            // Account for zero padding in size of DFT input array
            var totalInputData = new double[mLengthTotal];
            Array.Copy(timeSeries, totalInputData, timeSeries.Length);

            Complex[] output;
            if (mOutOfMemory)
                output = Dft(totalInputData);
            else
                output = DftCached(totalInputData);

            return output;
        }

        #region Private DFT Implementation details

        /// <summary>
        /// A brute force DFT - Uses Task / Parallel pattern
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <returns>Complex[] result</returns>
        private Complex[] Dft(double[] timeSeries)
        {
            var n = mLengthTotal;
            var m = mLengthHalf;
            var re = new double[m];
            var im = new double[m];
            var result = new Complex[m];
            var sf = 2.0 * Math.PI / n;

            // Parallel.For(0, m, (j) =>
            for (uint j = 0; j < m; j++)
            {
                var a = j * sf;
                for (uint k = 0; k < n; k++)
                {
                    re[j] += timeSeries[k] * Math.Cos(a * k) * mDFTScale;
                    im[j] -= timeSeries[k] * Math.Sin(a * k) * mDFTScale;
                }

                result[j] = new Complex(re[j], im[j]);
            };

            // DC and Fs/2 Points are scaled differently, since they have only a real part
            result[0] = new Complex(result[0].Real / Math.Sqrt(2), 0.0);
            result[mLengthHalf - 1] = new Complex(result[mLengthHalf - 1].Real / Math.Sqrt(2), 0.0);

            return result;
        }

        /// <summary>
        /// DFT with Pre-calculated Sin/Cos arrays + Task / Parallel pattern.
        /// DFT can only be so big before the computer runs out of memory and has to use
        /// the brute force DFT.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <returns>Complex[] result</returns>
        private Complex[] DftCached(double[] timeSeries)
        {
            var n = mLengthTotal;
            var m = mLengthHalf;
            var re = new double[m];
            var im = new double[m];
            var result = new Complex[m];

            //Parallel.For(0, m, (j) =>
            for (uint j = 0; j < m; j++)
            {
                for (uint k = 0; k < n; k++)
                {
                    re[j] += timeSeries[k] * mCosTerm[j, k];
                    im[j] -= timeSeries[k] * mSinTerm[j, k];
                }
                result[j] = new Complex(re[j], im[j]);
            };

            // DC and Fs/2 Points are scaled differently, since they have only a real part
            result[0] = new Complex(result[0].Real / Math.Sqrt(2), 0.0);
            result[mLengthHalf - 1] = new Complex(result[mLengthHalf - 1].Real / Math.Sqrt(2), 0.0);

            return result;
        }

        #endregion

        #endregion

        #region Utility Functions

        /// <summary>
        /// Return the Frequency Array for the currently defined DFT.
        /// Takes into account the total number of points and zero padding points that were defined.
        /// </summary>
        /// <param name="samplingFrequencyHz"></param>
        /// <returns></returns>
        public double[] FrequencySpan(double samplingFrequencyHz)
        {
            var points = mLengthHalf;
            var result = new double[points];
            var stopValue = samplingFrequencyHz / 2.0;
            var increment = stopValue / (points - 1.0);

            for (uint i = 0; i < points; i++)
                result[i] += increment * i;

            return result;
        }

    }

    #endregion

    #endregion


    #region =====[ FFT Core Class ]======================================================

    /**
     * Performs an in-place complex FFT.
     *
     * Released under the MIT License
     *
     * Core FFT class based on,
     *      Fast C# FFT - Copyright (c) 2010 Gerald T. Beauregard
     *
     * Changes to: Interface, scaling, zero padding, return values.
     * Change to .NET Complex output types and integrated with my DSP Library.
     * Note: Complex Number Type requires .NET >= 4.0
     *
     * These changes as noted above Copyright (c) 2016 Steven C. Hageman
     *
     *
     * Permission is hereby granted, free of charge, to any person obtaining a copy
     * of this software and associated documentation files (the "Software"), to
     * deal in the Software without restriction, including without limitation the
     * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
     * sell copies of the Software, and to permit persons to whom the Software is
     * furnished to do so, subject to the following conditions:
     *
     * The above copyright notice and this permission notice shall be included in
     * all copies or substantial portions of the Software.
     *
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
     * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
     * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
     * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
     * IN THE SOFTWARE.
     */


    /// <summary>
    /// FFT Base Class
    /// </summary>
    [SuppressMessage("Style", "IDE0047:Remove unnecessary parentheses",
        Justification = "External Library, should not be enforced by editorconfig")]
    public class FFT
    {
        /// <summary>
        /// FFT Class
        /// </summary>
        public FFT() { }

        #region Private Properties

        private double mFFTScale = 1.0;
        private uint mLogN = 0;       // log2 of FFT size
        private uint mN = 0;          // Time series length
        private uint mLengthTotal;    // mN + mZp
        private uint mLengthHalf;     // (mN + mZp) / 2
        private FFTElement[] mX;        // Vector of linked list elements

        // Element for linked list to store input/output data.
        private class FFTElement
        {
            public double Re = 0.0;     // Real component
            public double Im = 0.0;     // Imaginary component
            public FFTElement Next;     // Next element in linked list
            public uint RevTgt;       // Target position post bit-reversal
        }

        #endregion

        #region FFT Core Functions

        /// <summary>
        /// Initialize the FFT. Must call first and this anytime the FFT setup changes.
        /// </summary>
        /// <param name="inputDataLength"></param>
        /// <param name="zeroPaddingLength"></param>
        public void Initialize(uint inputDataLength, uint zeroPaddingLength = 0)
        {
            mN = inputDataLength;

            // Find the power of two for the total FFT size up to 2^32
            var foundIt = false;
            for (mLogN = 1; mLogN <= 32; mLogN++)
            {
                var n = Math.Pow(2.0, mLogN);
                if ((inputDataLength + zeroPaddingLength) == n)
                {
                    foundIt = true;
                    break;
                }
            }

            if (foundIt == false)
                throw new ArgumentOutOfRangeException("inputDataLength + zeroPaddingLength was not an even power of 2! FFT cannot continue.");

            // Set global parameters.
            mLengthTotal = inputDataLength + zeroPaddingLength;
            mLengthHalf = (mLengthTotal / 2) + 1;

            // Set the overall scale factor for all the terms
            mFFTScale = Math.Sqrt(2) / mLengthTotal;                // Natural FFT Scale Factor                                           // Window Scale Factor
            mFFTScale *= mLengthTotal / (double)inputDataLength;    // Zero Padding Scale Factor

            // Allocate elements for linked list of complex numbers.
            mX = new FFTElement[mLengthTotal];
            for (uint k = 0; k < (mLengthTotal); k++)
                mX[k] = new FFTElement();

            // Set up "next" pointers.
            for (uint k = 0; k < (mLengthTotal) - 1; k++)
                mX[k].Next = mX[k + 1];

            // Specify target for bit reversal re-ordering.
            for (uint k = 0; k < (mLengthTotal); k++)
                mX[k].RevTgt = BitReverse(k, mLogN);
        }


        /// <summary>
        /// Executes a FFT of the input time series.
        /// </summary>
        /// <param name="timeSeries"></param>
        /// <returns>Complex[] Spectrum</returns>
        public Complex[] Execute(double[] timeSeries)
        {
            var numFlies = mLengthTotal >> 1;  // Number of butterflies per sub-FFT
            var span = mLengthTotal >> 1;      // Width of the butterfly
            var spacing = mLengthTotal;        // Distance between start of sub-FFTs
            uint wIndexStep = 1;          // Increment for twiddle table index

            Debug.Assert(timeSeries.Length <= mLengthTotal, "The input timeSeries length was greater than the total number of points that was initialized. FFT.Exectue()");

            // Copy data into linked complex number objects
            var x = mX[0];
            uint k = 0;
            for (uint i = 0; i < mN; i++)
            {
                x.Re = timeSeries[k];
                x.Im = 0.0;
                x = x.Next;
                k++;
            }

            // If zero padded, clean the 2nd half of the linked list from previous results
            if (mN != mLengthTotal)
            {
                for (var i = mN; i < mLengthTotal; i++)
                {
                    x.Re = 0.0;
                    x.Im = 0.0;
                    x = x.Next;
                }
            }

            // For each stage of the FFT
            for (uint stage = 0; stage < mLogN; stage++)
            {
                // Compute a multiplier factor for the "twiddle factors".
                // The twiddle factors are complex unit vectors spaced at
                // regular angular intervals. The angle by which the twiddle
                // factor advances depends on the FFT stage. In many FFT
                // implementations the twiddle factors are cached, but because
                // array lookup is relatively slow in C#, it's just
                // as fast to compute them on the fly.
                var wAngleInc = wIndexStep * -2.0 * Math.PI / (mLengthTotal);
                var wMulRe = Math.Cos(wAngleInc);
                var wMulIm = Math.Sin(wAngleInc);

                for (uint start = 0; start < (mLengthTotal); start += spacing)
                {
                    var xTop = mX[start];
                    var xBot = mX[start + span];

                    var wRe = 1.0;
                    var wIm = 0.0;

                    // For each butterfly in this stage
                    for (uint flyCount = 0; flyCount < numFlies; ++flyCount)
                    {
                        // Get the top & bottom values
                        var xTopRe = xTop.Re;
                        var xTopIm = xTop.Im;
                        var xBotRe = xBot.Re;
                        var xBotIm = xBot.Im;

                        // Top branch of butterfly has addition
                        xTop.Re = xTopRe + xBotRe;
                        xTop.Im = xTopIm + xBotIm;

                        // Bottom branch of butterfly has subtraction,
                        // followed by multiplication by twiddle factor
                        xBotRe = xTopRe - xBotRe;
                        xBotIm = xTopIm - xBotIm;
                        xBot.Re = (xBotRe * wRe) - (xBotIm * wIm);
                        xBot.Im = (xBotRe * wIm) + (xBotIm * wRe);

                        // Advance butterfly to next top & bottom positions
                        xTop = xTop.Next;
                        xBot = xBot.Next;

                        // Update the twiddle factor, via complex multiply
                        // by unit vector with the appropriate angle
                        // (wRe + j wIm) = (wRe + j wIm) x (wMulRe + j wMulIm)
                        var tRe = wRe;
                        wRe = (wRe * wMulRe) - (wIm * wMulIm);
                        wIm = (tRe * wMulIm) + (wIm * wMulRe);
                    }
                }

                numFlies >>= 1;   // Divide by 2 by right shift
                span >>= 1;
                spacing >>= 1;
                wIndexStep <<= 1;     // Multiply by 2 by left shift
            }

            // The algorithm leaves the result in a scrambled order.
            // Unscramble while copying values from the complex
            // linked list elements to a complex output vector & properly apply scale factors.

            x = mX[0];
            var unswizzle = new Complex[mLengthTotal];
            while (x != null)
            {
                var target = x.RevTgt;
                unswizzle[target] = new Complex(x.Re * mFFTScale, x.Im * mFFTScale);
                x = x.Next;
            }

            // Return 1/2 the FFT result from DC to Fs/2 (The real part of the spectrum)
            //UInt32 halfLength = ((mN + mZp) / 2) + 1;
            var result = new Complex[mLengthHalf];
            Array.Copy(unswizzle, result, mLengthHalf);

            // DC and Fs/2 Points are scaled differently, since they have only a real part
            result[0] = new Complex(result[0].Real / Math.Sqrt(2), 0.0);
            result[mLengthHalf - 1] = new Complex(result[mLengthHalf - 1].Real / Math.Sqrt(2), 0.0);

            return result;
        }

        #region Private FFT Routines

        /**
         * Do bit reversal of specified number of places of an int
         * For example, 1101 bit-reversed is 1011
         *
         * @param   x       Number to be bit-reverse.
         * @param   numBits Number of bits in the number.
         */
        private uint BitReverse(uint x, uint numBits)
        {
            uint y = 0;
            for (uint i = 0; i < numBits; i++)
            {
                y <<= 1;
                y |= x & 0x0001;
                x >>= 1;
            }
            return y;
        }

        #endregion

        #endregion

        #region Utility Functions

        /// <summary>
        /// Return the Frequency Array for the currently defined FFT.
        /// Takes into account the total number of points and zero padding points that were defined.
        /// </summary>
        /// <param name="samplingFrequencyHz"></param>
        /// <returns></returns>
        public double[] FrequencySpan(double samplingFrequencyHz)
        {
            var points = mLengthHalf;
            var result = new double[points];
            var stopValue = samplingFrequencyHz / 2.0;
            var increment = stopValue / (points - 1.0);

            for (var i = 0; i < points; i++)
                result[i] += increment * i;

            return result;
        }

        #endregion

    }

    #endregion


    #region =====[ Generation, Conversion, Analysis and Array Manipulations ]============

    public class DSP
    {
        /*
        * Released under the MIT License
        *
        * DSP Library for C# - Copyright(c) 2016 Steven C. Hageman.
        *
        * Permission is hereby granted, free of charge, to any person obtaining a copy
        * of this software and associated documentation files (the "Software"), to
        * deal in the Software without restriction, including without limitation the
        * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
        * sell copies of the Software, and to permit persons to whom the Software is
        * furnished to do so, subject to the following conditions:
        *
        * The above copyright notice and this permission notice shall be included in
        * all copies or substantial portions of the Software.
        *
        * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
        * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
        * IN THE SOFTWARE.
        */


        #region Generate Signals & Noise

        public static class Generate
        {
            /// <summary>
            /// Generate linearly spaced array. Like the Octave function of the same name.
            /// EX: DSP.Generate.LinSpace(1, 10, 10) -> Returns array: 1, 2, 3, 4....10.
            /// </summary>
            /// <param name="startVal">Any value</param>
            /// <param name="stopVal">Any value > startVal</param>
            /// <param name="points">Number of points to generate</param>
            /// <returns>double[] array</returns>
            public static double[] LinSpace(double startVal, double stopVal, uint points)
            {
                var result = new double[points];
                var increment = (stopVal - startVal) / (points - 1.0);

                for (uint i = 0; i < points; i++)
                    result[i] = startVal + (increment * i);

                return result;
            }


            /// <summary>
            /// Generates a Sine Wave Tone using Sampling Terms.
            /// </summary>
            /// <param name="amplitudeVrms"></param>
            /// <param name="frequencyHz"></param>
            /// <param name="samplingFrequencyHz"></param>
            /// <param name="points"></param>
            /// <param name="dcV">[Optional] DC Voltage offset</param>
            /// <param name="phaseDeg">[Optional] Phase of signal in degrees</param>
            /// <returns>double[] array</returns>
            public static double[] ToneSampling(double amplitudeVrms, double frequencyHz, double samplingFrequencyHz, uint points, double dcV = 0.0, double phaseDeg = 0)
            {
                var ph_r = phaseDeg * System.Math.PI / 180.0;

                var rval = new double[points];
                for (uint i = 0; i < points; i++)
                {
                    var time = i / samplingFrequencyHz;
                    rval[i] = (System.Math.Sqrt(2) * amplitudeVrms * System.Math.Sin((2.0 * System.Math.PI * time * frequencyHz) + ph_r)) + dcV;
                }
                return rval;
            }


            /// <summary>
            /// Generates a Sine Wave Tone using Number of Cycles Terms.
            /// </summary>
            /// <param name="amplitudeVrms"></param>
            /// <param name="cycles"></param>
            /// <param name="points"></param>
            /// <param name="dcV">[Optional] DC Voltage offset</param>
            /// <param name="phaseDeg">[Optional] Phase of signal in degrees</param>
            /// <returns>double[] array</returns>
            public static double[] ToneCycles(double amplitudeVrms, double cycles, uint points, double dcV = 0.0, double phaseDeg = 0)
            {
                var ph_r = phaseDeg * System.Math.PI / 180.0;
                var ampPeak = System.Math.Sqrt(2) * amplitudeVrms;

                var rval = new double[points];
                for (uint i = 0; i < points; i++)
                {
                    rval[i] = (ampPeak * System.Math.Sin((2.0 * System.Math.PI * i / points * cycles) + ph_r)) + dcV;
                }
                return rval;
            }


            /// <summary>
            /// Generates a normal distribution noise signal of the specified power spectral density (Vrms / rt-Hz).
            /// </summary>
            /// <param name="amplitudePsd (Vrms / rt-Hz)"></param>
            /// <param name="samplingFrequencyHz"></param>
            /// <param name="points"></param>
            /// <returns>double[] array</returns>
            public static double[] NoisePsd(double amplitudePsd, double samplingFrequencyHz, uint points)
            {
                // Calculate what the noise amplitude needs to be in Vrms/rt_Hz
                var arms = amplitudePsd * System.Math.Sqrt(samplingFrequencyHz / 2.0);

                // Make an n length noise vector
                var rval = NoiseRms(arms, points);

                return rval;
            }



            /// <summary>
            /// Generates a normal distribution noise signal of the specified Volts RMS.
            /// </summary>
            /// <param name="amplitudeVrms"></param>
            /// <param name="points"></param>
            /// <param name="dcV"></param>
            /// <returns>double[] array</returns>
            public static double[] NoiseRms(double amplitudeVrms, uint points, double dcV = 0.0)
            {
                // Make an n length noise vector
                var rval = Noise(points, amplitudeVrms);

                rval = Math.Add(rval, dcV);

                return rval;
            }

            #region Private - Random Number Generator Core

            //=====[ Gaussian Noise ]=====

            private static readonly Random mRandom = new Random(); // Class level variable

            private static double[] Noise(uint size, double scaling_vrms)
            {

                // Based on - Polar method (Marsaglia 1962)

                // Scaling used,
                // * For DFT Size => "Math.Sqrt(size)"
                // * The Sqrt(2) is a scaling factor to get the
                // output spectral power to be what the desired "scaling_vrms"
                // was as requested. The scaling will produce a "scaling_vrms"
                // value that is correct for Vrms/Rt(Hz) in the frequency domain
                // of a "1/N" scaled DFT or FFT.
                // Most DFT / FFT's are 1/N scaled - check your documentation to be sure...

                var output_scale = scaling_vrms;

                var data = new double[size];
                double sum = 0;

                for (uint n = 0; n < size; n++)
                {
                    double s;
                    double v1;
                    do
                    {
                        v1 = (2.0 * mRandom.NextDouble()) - 1.0;
                        var v2 = (2.0 * mRandom.NextDouble()) - 1.0;

                        s = (v1 * v1) + (v2 * v2);
                    } while (s >= 1.0);

                    if (s == 0.0)
                        data[n] = 0.0;
                    else
                        data[n] = v1 * System.Math.Sqrt(-2.0 * System.Math.Log(s) / s) * output_scale;

                    sum += data[n];
                }

                // Remove the average value
                var average = sum / size;
                for (uint n = 0; n < size; n++)
                {
                    data[n] -= average;
                }

                // Return the Gaussian noise
                return data;
            }

            #endregion
        }

        #endregion


        #region Windows Functions & Scaling Functions

        /**
        *
        * Many of the windows functions are based on the article,
        *
        *   Spectrum and spectral density estimation by the Discrete Fourier
        *   transform (DFT), including a comprehensive list of window
        *   functions and some new ﬂat-top windows.
        *
        *   G. Heinzel, A. Rudiger and R. Schilling,
        *   Max-Planck-Institut fur Gravitationsphysik
        *
        *   February 15, 2002
        *
        **/

        public static class Window
        {
            /// <summary>
            /// ENUM Types for included Windows.
            /// </summary>
            public enum Type
            {
                None,
                Rectangular,
                Welch,
                Bartlett,
                Hanning,
                Hann,
                Hamming,
                Nutall3,
                Nutall4,
                Nutall3A,
                Nutall3B,
                Nutall4A,
                BH92,
                Nutall4B,

                SFT3F,
                SFT3M,
                FTNI,
                SFT4F,
                SFT5F,
                SFT4M,
                FTHP,
                HFT70,
                FTSRS,
                SFT5M,
                HFT90D,
                HFT95,
                HFT116D,
                HFT144D,
                HFT169D,
                HFT196D,
                HFT223D,
                HFT248D
            }

            #region Window Scale Factor

            public static class ScaleFactor
            {
                /// <summary>
                /// Calculate Signal scale factor from window coefficient array.
                /// Designed to be applied to the "Magnitude" result.
                /// </summary>
                /// <param name="windowCoefficients"></param>
                /// <returns>double scaleFactor</returns>
                public static double Signal(double[] windowCoefficients)
                {
                    double s1 = 0;
                    foreach (var coeff in windowCoefficients)
                    {
                        s1 += coeff;
                    }

                    s1 /= windowCoefficients.Length;

                    return 1.0 / s1;
                }


                /// <summary>
                ///  Calculate Noise scale factor from window coefficient array.
                ///  Takes into account the bin width in Hz for the final result also.
                ///  Designed to be applied to the "Magnitude" result.
                /// </summary>
                /// <param name="windowCoefficients"></param>
                /// <param name="samplingFrequencyHz"></param>
                /// <returns>double scaleFactor</returns>
                public static double Noise(double[] windowCoefficients, double samplingFrequencyHz)
                {
                    double s2 = 0;
                    foreach (var coeff in windowCoefficients)
                    {
                        s2 += coeff * coeff;
                    }

                    double n = windowCoefficients.Length;
                    var fbin = samplingFrequencyHz / n;

                    var sf = System.Math.Sqrt(1.0 / (s2 / n * fbin));

                    return sf;
                }


                /// <summary>
                ///  Calculate Normalized, Equivalent Noise BandWidth from window coefficient array.
                /// </summary>
                /// <param name="windowCoefficients"></param>
                /// <returns>double NENBW</returns>
                public static double NENBW(double[] windowCoefficients)
                {
                    double s1 = 0;
                    double s2 = 0;
                    foreach (var coeff in windowCoefficients)
                    {
                        s1 += coeff;
                        s2 += coeff * coeff;
                    }

                    double n = windowCoefficients.Length;
                    s1 /= n;

                    var nenbw = s2 / (s1 * s1) / n;

                    return nenbw;
                }
            }
            #endregion

            #region Window Coefficient Calculations

            /// <summary>
            /// Calculates a set of Windows coefficients for a given number of points and a window type to use.
            /// </summary>
            /// <param name="windowName"></param>
            /// <param name="points"></param>
            /// <returns>double[] array of the calculated window coefficients</returns>
            public static double[] Coefficients(Type windowName, uint points)
            {
                var winCoeffs = new double[points];
                double n = points;

                switch (windowName)
                {
                    case Type.None:
                    case Type.Rectangular:
                        //wc = ones(N,1);
                        for (uint i = 0; i < points; i++)
                            winCoeffs[i] = 1.0;

                        break;

                    case Type.Bartlett:
                        //n = (0:N-1)';
                        //wc = 2/N*(N/2-abs(n-(N-1)/2));
                        for (uint i = 0; i < points; i++)
                            winCoeffs[i] = 2.0 / n * ((n / 2.0) - System.Math.Abs(i - ((n - 1.0) / 2.0)));

                        break;

                    case Type.Welch:
                        //n = (0:N-1)';
                        //wc = 1 - ( ((2*n)/N) - 1).^2;
                        for (uint i = 0; i < points; i++)
                            winCoeffs[i] = 1.0 - System.Math.Pow((2.0 * i / n) - 1.0, 2.0);
                        break;

                    case Type.Hann:
                    case Type.Hanning:
                        //wc = (0.5 - 0.5*cos (z));
                        winCoeffs = SineExpansion(points, 0.5, -0.5);
                        break;

                    case Type.Hamming:
                        //wc = (0.54 - 0.46*cos (z));
                        winCoeffs = SineExpansion(points, 0.54, -0.46);
                        break;

                    case Type.BH92: // Also known as: Blackman-Harris
                                    //wc = (0.35875 - 0.48829*cos(z) + 0.14128*cos(2*z) - 0.01168*cos(3*z));
                        winCoeffs = SineExpansion(points, 0.35875, -0.48829, 0.14128, -0.01168);
                        break;

                    case Type.Nutall3:
                        //c0 = 0.375; c1 = -0.5; c2 = 0.125;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z);
                        winCoeffs = SineExpansion(points, 0.375, -0.5, 0.125);
                        break;

                    case Type.Nutall3A:
                        //c0 = 0.40897; c1 = -0.5; c2 = 0.09103;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z);
                        winCoeffs = SineExpansion(points, 0.40897, -0.5, 0.09103);
                        break;

                    case Type.Nutall3B:
                        //c0 = 0.4243801; c1 = -0.4973406; c2 = 0.0782793;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z);
                        winCoeffs = SineExpansion(points, 0.4243801, -0.4973406, 0.0782793);
                        break;

                    case Type.Nutall4:
                        //c0 = 0.3125; c1 = -0.46875; c2 = 0.1875; c3 = -0.03125;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z);
                        winCoeffs = SineExpansion(points, 0.3125, -0.46875, 0.1875, -0.03125);
                        break;

                    case Type.Nutall4A:
                        //c0 = 0.338946; c1 = -0.481973; c2 = 0.161054; c3 = -0.018027;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z);
                        winCoeffs = SineExpansion(points, 0.338946, -0.481973, 0.161054, -0.018027);
                        break;

                    case Type.Nutall4B:
                        //c0 = 0.355768; c1 = -0.487396; c2 = 0.144232; c3 = -0.012604;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z);
                        winCoeffs = SineExpansion(points, 0.355768, -0.487396, 0.144232, -0.012604);
                        break;

                    case Type.SFT3F:
                        //c0 = 0.26526; c1 = -0.5; c2 = 0.23474;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z);
                        winCoeffs = SineExpansion(points, 0.26526, -0.5, 0.23474);
                        break;

                    case Type.SFT4F:
                        //c0 = 0.21706; c1 = -0.42103; c2 = 0.28294; c3 = -0.07897;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z);
                        winCoeffs = SineExpansion(points, 0.21706, -0.42103, 0.28294, -0.07897);
                        break;

                    case Type.SFT5F:
                        //c0 = 0.1881; c1 = -0.36923; c2 = 0.28702; c3 = -0.13077; c4 = 0.02488;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z) + c4*cos(4*z);
                        winCoeffs = SineExpansion(points, 0.1881, -0.36923, 0.28702, -0.13077, 0.02488);
                        break;

                    case Type.SFT3M:
                        //c0 = 0.28235; c1 = -0.52105; c2 = 0.19659;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z);
                        winCoeffs = SineExpansion(points, 0.28235, -0.52105, 0.19659);
                        break;

                    case Type.SFT4M:
                        //c0 = 0.241906; c1 = -0.460841; c2 = 0.255381; c3 = -0.041872;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z);
                        winCoeffs = SineExpansion(points, 0.241906, -0.460841, 0.255381, -0.041872);
                        break;

                    case Type.SFT5M:
                        //c0 = 0.209671; c1 = -0.407331; c2 = 0.281225; c3 = -0.092669; c4 = 0.0091036;
                        //wc = c0 + c1*cos(z) + c2*cos(2*z) + c3*cos(3*z) + c4*cos(4*z);
                        winCoeffs = SineExpansion(points, 0.209671, -0.407331, 0.281225, -0.092669, 0.0091036);
                        break;

                    case Type.FTNI:
                        //wc = (0.2810639 - 0.5208972*cos(z) + 0.1980399*cos(2*z));
                        winCoeffs = SineExpansion(points, 0.2810639, -0.5208972, 0.1980399);
                        break;

                    case Type.FTHP:
                        //wc = 1.0 - 1.912510941*cos(z) + 1.079173272*cos(2*z) - 0.1832630879*cos(3*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.912510941, 1.079173272, -0.1832630879);
                        break;

                    case Type.HFT70:
                        //wc = 1 - 1.90796*cos(z) + 1.07349*cos(2*z) - 0.18199*cos(3*z);
                        winCoeffs = SineExpansion(points, 1, -1.90796, 1.07349, -0.18199);
                        break;

                    case Type.FTSRS:
                        //wc = 1.0 - 1.93*cos(z) + 1.29*cos(2*z) - 0.388*cos(3*z) + 0.028*cos(4*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.93, 1.29, -0.388, 0.028);
                        break;

                    case Type.HFT90D:
                        //wc = 1 - 1.942604*cos(z) + 1.340318*cos(2*z) - 0.440811*cos(3*z) + 0.043097*cos(4*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.942604, 1.340318, -0.440811, 0.043097);
                        break;

                    case Type.HFT95:
                        //wc = 1 - 1.9383379*cos(z) + 1.3045202*cos(2*z) - 0.4028270*cos(3*z) + 0.0350665*cos(4*z);
                        winCoeffs = SineExpansion(points, 1, -1.9383379, 1.3045202, -0.4028270, 0.0350665);
                        break;

                    case Type.HFT116D:
                        //wc = 1 - 1.9575375*cos(z) + 1.4780705*cos(2*z) - 0.6367431*cos(3*z) + 0.1228389*cos(4*z) - 0.0066288*cos(5*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.9575375, 1.4780705, -0.6367431, 0.1228389, -0.0066288);
                        break;

                    case Type.HFT144D:
                        //wc = 1 - 1.96760033*cos(z) + 1.57983607*cos(2*z) - 0.81123644*cos(3*z) + 0.22583558*cos(4*z) - 0.02773848*cos(5*z) + 0.00090360*cos(6*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.96760033, 1.57983607, -0.81123644, 0.22583558, -0.02773848, 0.00090360);
                        break;

                    case Type.HFT169D:
                        //wc = 1 - 1.97441842*cos(z) + 1.65409888*cos(2*z) - 0.95788186*cos(3*z) + 0.33673420*cos(4*z) - 0.06364621*cos(5*z) + 0.00521942*cos(6*z) - 0.00010599*cos(7*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.97441842, 1.65409888, -0.95788186, 0.33673420, -0.06364621, 0.00521942, -0.00010599);
                        break;

                    case Type.HFT196D:
                        //wc = 1 - 1.979280420*cos(z) + 1.710288951*cos(2*z) - 1.081629853*cos(3*z)+ 0.448734314*cos(4*z) - 0.112376628*cos(5*z) + 0.015122992*cos(6*z) - 0.000871252*cos(7*z) + 0.000011896*cos(8*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.979280420, 1.710288951, -1.081629853, 0.448734314, -0.112376628, 0.015122992, -0.000871252, 0.000011896);
                        break;

                    case Type.HFT223D:
                        //wc = 1 - 1.98298997309*cos(z) + 1.75556083063*cos(2*z) - 1.19037717712*cos(3*z) + 0.56155440797*cos(4*z) - 0.17296769663*cos(5*z) + 0.03233247087*cos(6*z) - 0.00324954578*cos(7*z) + 0.00013801040*cos(8*z) - 0.00000132725*cos(9*z);
                        winCoeffs = SineExpansion(points, 1.0, -1.98298997309, 1.75556083063, -1.19037717712, 0.56155440797, -0.17296769663, 0.03233247087, -0.00324954578, 0.00013801040, -0.00000132725);
                        break;

                    case Type.HFT248D:
                        //wc = 1 - 1.985844164102*cos(z) + 1.791176438506*cos(2*z) - 1.282075284005*cos(3*z) + 0.667777530266*cos(4*z) - 0.240160796576*cos(5*z) + 0.056656381764*cos(6*z) - 0.008134974479*cos(7*z) + 0.000624544650*cos(8*z) - 0.000019808998*cos(9*z) + 0.000000132974*cos(10*z);
                        winCoeffs = SineExpansion(points, 1, -1.985844164102, 1.791176438506, -1.282075284005, 0.667777530266, -0.240160796576, 0.056656381764, -0.008134974479, 0.000624544650, -0.000019808998, 0.000000132974);
                        break;

                    default:
                        //throw new NotImplementedException("Window type fell through to 'Default'.");
                        break;
                }

                return winCoeffs;
            }

            private static double[] SineExpansion(uint points, double c0, double c1 = 0.0, double c2 = 0.0, double c3 = 0.0, double c4 = 0.0, double c5 = 0.0, double c6 = 0.0, double c7 = 0.0, double c8 = 0.0, double c9 = 0.0, double c10 = 0.0)
            {
                // z = 2 * pi * (0:N-1)' / N;   // Cosine Vector
                var z = new double[points];
                for (uint i = 0; i < points; i++)
                    z[i] = 2.0 * System.Math.PI * i / points;

                var winCoeffs = new double[points];

                for (uint i = 0; i < points; i++)
                {
                    var wc = c0;
                    wc += c1 * System.Math.Cos(z[i]);
                    wc += c2 * System.Math.Cos(2.0 * z[i]);
                    wc += c3 * System.Math.Cos(3.0 * z[i]);
                    wc += c4 * System.Math.Cos(4.0 * z[i]);
                    wc += c5 * System.Math.Cos(5.0 * z[i]);
                    wc += c6 * System.Math.Cos(6.0 * z[i]);
                    wc += c7 * System.Math.Cos(7.0 * z[i]);
                    wc += c8 * System.Math.Cos(8.0 * z[i]);
                    wc += c9 * System.Math.Cos(9.0 * z[i]);
                    wc += c10 * System.Math.Cos(10.0 * z[i]);

                    winCoeffs[i] = wc;
                }

                return winCoeffs;
            }

            #endregion

        }

        #endregion


        #region Convert Magnitude format to user friendly formats

        /// <summary>
        /// DFT / FFT Format Conversion Functions
        /// </summary>
        public static class ConvertMagnitude
        {
            /// <summary>
            /// Convert Magnitude FT Result to: Magnitude Squared Format
            /// </summary>
            /// <param name="magnitude"></param>
            /// <returns></returns>
            public static double[] ToMagnitudeSquared(double[] magnitude)
            {
                var np = (uint)magnitude.Length;
                var mag2 = new double[np];
                for (uint i = 0; i < np; i++)
                {
                    mag2[i] = magnitude[i] * magnitude[i];
                }

                return mag2;
            }


            /// <summary>
            /// Convert Magnitude FT Result to: Magnitude dBVolts
            /// </summary>
            /// <param name="magnitude"></param>
            /// <returns>double[] array</returns>
            public static double[] ToMagnitudeDBV(double[] magnitude)
            {
                var np = (uint)magnitude.Length;
                var magDBV = new double[np];
                for (uint i = 0; i < np; i++)
                {
                    var magVal = magnitude[i];
                    if (magVal <= 0.0)
                        magVal = double.Epsilon;

                    magDBV[i] = 20 * System.Math.Log10(magVal);
                }

                return magDBV;
            }

        }

        #endregion


        #region Convert Magnitude Squared format to user friendly formats

        /// <summary>
        /// DFT / FFT Format Conversion Functions
        /// </summary>
        public static class ConvertMagnitudeSquared
        {

            /// <summary>
            /// Convert Magnitude Squared FFT Result to: Magnitude Vrms
            /// </summary>
            /// <param name="magSquared"></param>
            /// <returns>double[] array</returns>
            public static double[] ToMagnitude(double[] magSquared)
            {
                var np = (uint)magSquared.Length;
                var mag = new double[np];
                for (uint i = 0; i < np; i++)
                {
                    mag[i] = System.Math.Sqrt(magSquared[i]);
                }

                return mag;
            }

            /// <summary>
            /// Convert Magnitude Squared FFT Result to: Magnitude dBVolts
            /// </summary>
            /// <param name="magSquared"></param>
            /// <returns>double[] array</returns>
            public static double[] ToMagnitudeDBV(double[] magSquared)
            {
                var np = (uint)magSquared.Length;
                var magDBV = new double[np];
                for (uint i = 0; i < np; i++)
                {
                    var magSqVal = magSquared[i];
                    if (magSqVal <= 0.0)
                        magSqVal = double.Epsilon;

                    magDBV[i] = 10 * System.Math.Log10(magSqVal);
                }

                return magDBV;
            }
        }

        #endregion


        #region Convert Complex format to user friendly formats

        /// <summary>
        /// DFT / FFT Format Conversion Functions.
        /// </summary>
        public static class ConvertComplex
        {
            /// <summary>
            /// Convert Complex DFT/FFT Result to: Magnitude Squared V^2 rms
            /// </summary>
            /// <param name="rawFFT"></param>
            /// <returns>double[] MagSquared Format</returns>
            public static double[] ToMagnitudeSquared(Complex[] rawFFT)
            {
                var np = (uint)rawFFT.Length;
                var magSquared = new double[np];
                for (uint i = 0; i < np; i++)
                {
                    var mag = rawFFT[i].Magnitude;
                    magSquared[i] = mag * mag;
                }

                return magSquared;
            }


            /// <summary>
            /// Convert Complex DFT/FFT Result to: Magnitude Vrms
            /// </summary>
            /// <param name="rawFFT"></param>
            /// <returns>double[] Magnitude Format (Vrms)</returns>
            public static double[] ToMagnitude(Complex[] rawFFT)
            {
                var np = (uint)rawFFT.Length;
                var mag = new double[np];
                for (uint i = 0; i < np; i++)
                {
                    mag[i] = rawFFT[i].Magnitude;
                }

                return mag;
            }


            /// <summary>
            /// Convert Complex DFT/FFT Result to: Log Magnitude dBV
            /// </summary>
            /// <param name="rawFFT"> Complex[] input array"></param>
            /// <returns>double[] Magnitude Format (dBV)</returns>
            public static double[] ToMagnitudeDBV(Complex[] rawFFT)
            {
                var np = (uint)rawFFT.Length;
                var mag = new double[np];
                for (uint i = 0; i < np; i++)
                {
                    var magVal = rawFFT[i].Magnitude;

                    if (magVal <= 0.0)
                        magVal = double.Epsilon;

                    mag[i] = 20 * System.Math.Log10(magVal);
                }

                return mag;
            }


            /// <summary>
            /// Convert Complex DFT/FFT Result to: Phase in Degrees
            /// </summary>
            /// <param name="rawFFT"> Complex[] input array"></param>
            /// <returns>double[] Phase (Degrees)</returns>
            public static double[] ToPhaseDegrees(Complex[] rawFFT)
            {
                var sf = 180.0 / System.Math.PI; // Degrees per Radian scale factor

                var np = (uint)rawFFT.Length;
                var phase = new double[np];
                for (uint i = 0; i < np; i++)
                {
                    phase[i] = rawFFT[i].Phase * sf;
                }

                return phase;
            }


            /// <summary>
            /// Convert Complex DFT/FFT Result to: Phase in Radians
            /// </summary>
            /// <param name="rawFFT"> Complex[] input array"></param>
            /// <returns>double[] Phase (Degrees)</returns>
            public static double[] ToPhaseRadians(Complex[] rawFFT)
            {
                var np = (uint)rawFFT.Length;
                var phase = new double[np];
                for (uint i = 0; i < np; i++)
                {
                    phase[i] = rawFFT[i].Phase;
                }

                return phase;
            }
        }
        #endregion


        #region Analyze Spectrum Data

        /// <summary>
        /// DFT / FFT Output Analysis Functions
        /// </summary>
        public static class Analyze
        {
            /// <summary>
            /// Find the RMS value of a[].
            /// </summary>
            /// <param name="inData"> = of N data points, 0 based.</param>
            /// <param name="startBin"> = Bin to start the counting at (0 based)."></param>
            /// <param name="stopBin"> = Bin FROM END to stop counting at (Max = N - 1)."></param>
            /// <returns>RMS value of input array between start and stop bins.</returns>
            public static double FindRms(double[] a, uint startBin = 10, uint stopBin = 10)
            {
                var sum2 = 0.0;
                uint actualSumCount = 0;
                var n = (uint)a.Length;
                for (uint i = 0; i < n; i++)
                {
                    if (i <= startBin - 1)
                        continue;
                    if (i > n - 1 - stopBin)
                        continue;

                    sum2 += a[i] * a[i];
                    actualSumCount++;
                }

                var avg2 = sum2 / actualSumCount;
                var rms = System.Math.Sqrt(avg2);

                return rms;
            }


            /// <summary>
            /// Finds the mean of the input array.
            /// </summary>
            /// <param name="inData"> = of N data points, 0 based.</param>
            /// <param name="startBin"> = Bin to start the counting at (0 based)."></param>
            /// <param name="stopBin"> = Bin FROM END to stop counting at (Max = N - 1)."></param>
            /// <returns>Mean value of input array between start and stop bins.</returns>
            public static double FindMean(double[] inData, uint startBin = 10, uint stopBin = 10)
            {
                double sum = 0;
                double n = inData.Length;
                uint actualSumCount = 0;

                for (uint i = 0; i < n; i++)
                {
                    if (i <= startBin - 1)
                        continue;
                    if (i > n - 1 - stopBin)
                        continue;

                    sum += inData[i];
                    actualSumCount++;
                }
                return sum / actualSumCount;
            }


            /// <summary>
            /// Finds the maximum value in an array.
            /// </summary>
            /// <param name="inData"></param>
            /// <returns>Maximum value of input array</returns>
            public static double FindMaxAmplitude(double[] inData)
            {
                double n = inData.Length;
                var maxVal = -1e300;

                for (uint i = 0; i < n; i++)
                {
                    if (inData[i] > maxVal)
                    {
                        maxVal = inData[i];
                    }
                }

                return maxVal;
            }


            /// <summary>
            /// Finds the position in the inData array where the maximum value happens.
            /// </summary>
            /// <param name="inData"></param>
            /// <returns>Position of maximum value in input array</returns>
            public static uint FindMaxPosition(double[] inData)
            {
                double n = inData.Length;
                var maxVal = -1e300;
                uint maxIndex = 0;

                for (uint i = 0; i < n; i++)
                {
                    if (inData[i] > maxVal)
                    {
                        maxIndex = i;
                        maxVal = inData[i];
                    }
                }

                return maxIndex;
            }

            /// <summary>
            /// Finds the maximum frequency from the given inData and fSpan arrays.
            /// </summary>
            /// <param name="inData"></param>
            /// <param name="fSpan"></param>
            /// <returns>Maximum frequency from input arrays</returns>
            public static double FindMaxFrequency(double[] inData, double[] fSpan)
            {
                double n = inData.Length;
                var maxVal = -1e300;
                uint maxIndex = 0;

                for (uint i = 0; i < n; i++)
                {
                    if (inData[i] > maxVal)
                    {
                        maxIndex = i;
                        maxVal = inData[i];
                    }
                }

                return fSpan[maxIndex];
            }


            /// <summary>
            /// Unwraps the phase so that it is continuous, without jumps.
            /// </summary>
            /// <param name="inPhaseDeg">Array of Phase Data from FT in Degrees</param>
            /// <returns>Continuous Phase data</returns>
            public static double[] UnwrapPhaseDegrees(double[] inPhaseDeg)
            {
                var n = (uint)inPhaseDeg.Length;
                var unwrappedPhase = new double[n];

                var tempInData = new double[n];
                inPhaseDeg.CopyTo(tempInData, 0);

                // First point is unchanged
                unwrappedPhase[0] = tempInData[0];

                for (uint i = 1; i < n; i++)
                {
                    var delta = System.Math.Abs(tempInData[i - 1] - tempInData[i]);
                    if (delta >= 180)
                    {
                        // Phase jump!
                        if (tempInData[i - 1] < 0.0)
                        {
                            for (var j = i; j < n; j++)
                                tempInData[j] += -360;
                        }
                        else
                        {
                            for (var j = i; j < n; j++)
                                tempInData[j] += 360;
                        }
                    }
                    unwrappedPhase[i] = tempInData[i];
                }
                return unwrappedPhase;
            }


            /// <summary>
            /// Unwraps the phase so that it is continuous, without jumps.
            /// </summary>
            /// <param name="inPhaseRad">Array of Phase Data from FT in Radians</param>
            /// <returns>Continuous Phase data</returns>
            public static double[] UnwrapPhaseRadians(double[] inPhaseRad)
            {
                var pi = System.Math.PI;
                var twoPi = System.Math.PI * 2.0;

                var n = (uint)inPhaseRad.Length;

                var tempInData = new double[n];
                inPhaseRad.CopyTo(tempInData, 0);

                var unwrappedPhase = new double[n];

                // First point is unchanged
                unwrappedPhase[0] = tempInData[0];

                for (uint i = 1; i < n; i++)
                {
                    var delta = System.Math.Abs(tempInData[i - 1] - tempInData[i]);
                    if (delta >= pi)
                    {
                        // Phase jump!
                        if (tempInData[i - 1] < 0.0)
                        {
                            for (var j = i; j < n; j++)
                                tempInData[j] += -twoPi;
                        }
                        else
                        {
                            for (var j = i; j < n; j++)
                                tempInData[j] += twoPi;
                        }
                    }
                    unwrappedPhase[i] = tempInData[i];
                }
                return unwrappedPhase;
            }
        }

        #endregion


        #region Double[] Array Math Operators

        /// <summary>
        /// Double[] Array Math Operations (All Static)
        /// </summary>
        public static class Math
        {

            /// <summary>
            /// result[] = a[] * b[]
            /// </summary>
            public static double[] Multiply(double[] a, double[] b)
            {
                Debug.Assert(a.Length == b.Length, "Length of arrays a[] and b[] must match.");

                var result = new double[a.Length];
                for (uint i = 0; i < a.Length; i++)
                    result[i] = a[i] * b[i];

                return result;
            }

            /// <summary>
            /// result[] = a[] * b
            /// </summary>
            public static double[] Multiply(double[] a, double b)
            {
                var result = new double[a.Length];
                for (uint i = 0; i < a.Length; i++)
                    result[i] = a[i] * b;

                return result;
            }

            /// <summary>
            /// result[] = a[] + b[]
            /// </summary>
            public static double[] Add(double[] a, double[] b)
            {
                Debug.Assert(a.Length == b.Length, "Length of arrays a[] and b[] must match.");

                var result = new double[a.Length];
                for (uint i = 0; i < a.Length; i++)
                    result[i] = a[i] + b[i];

                return result;
            }

            /// <summary>
            /// result[] = a[] + b
            /// </summary>
            public static double[] Add(double[] a, double b)
            {
                var result = new double[a.Length];
                for (uint i = 0; i < a.Length; i++)
                    result[i] = a[i] + b;

                return result;
            }

            /// <summary>
            /// result[] = a[] - b[]
            /// </summary>
            public static double[] Subtract(double[] a, double[] b)
            {
                Debug.Assert(a.Length == b.Length, "Length of arrays a[] and b[] must match.");

                var result = new double[a.Length];
                for (uint i = 0; i < a.Length; i++)
                    result[i] = a[i] - b[i];

                return result;
            }

            /// <summary>
            /// result[] = a[] - b
            /// </summary>
            public static double[] Subtract(double[] a, double b)
            {
                var result = new double[a.Length];
                for (uint i = 0; i < a.Length; i++)
                    result[i] = a[i] - b;

                return result;
            }

            /// <summary>
            /// result[] = a[] / b[]
            /// </summary>
            public static double[] Divide(double[] a, double[] b)
            {
                Debug.Assert(a.Length == b.Length, "Length of arrays a[] and b[] must match.");

                var result = new double[a.Length];
                for (uint i = 0; i < a.Length; i++)
                    result[i] = a[i] / b[i];

                return result;
            }

            /// <summary>
            /// result[] = a[] / b
            /// </summary>
            public static double[] Divide(double[] a, double b)
            {
                var result = new double[a.Length];
                for (uint i = 0; i < a.Length; i++)
                    result[i] = a[i] / b;

                return result;
            }

            /// <summary>
            /// Square root of a[].
            /// </summary>
            public static double[] Sqrt(double[] a)
            {
                var result = new double[a.Length];
                for (uint i = 0; i < a.Length; i++)
                    result[i] = System.Math.Sqrt(a[i]);

                return result;
            }

            /// <summary>
            /// Squares a[].
            /// </summary>
            public static double[] Square(double[] a)
            {
                var result = new double[a.Length];
                for (uint i = 0; i < a.Length; i++)
                    result[i] = a[i] * a[i];

                return result;
            }

            /// <summary>
            /// Log10 a[].
            /// </summary>
            public static double[] Log10(double[] a)
            {
                var result = new double[a.Length];
                for (uint i = 0; i < a.Length; i++)
                {
                    var val = a[i];
                    if (val <= 0.0)
                        val = double.Epsilon;

                    result[i] = System.Math.Log10(val);
                }

                return result;
            }

            /// <summary>
            /// Removes mean value from a[].
            /// </summary>
            public static double[] RemoveMean(double[] a)
            {
                var sum = 0.0;
                for (uint i = 0; i < a.Length; i++)
                    sum += a[i];

                var mean = sum / a.Length;

                return Subtract(a, mean);
            }

        }

        #endregion

    }
    #endregion

}
