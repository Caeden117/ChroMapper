#region License and Information
/*****
*
* This is an implementation of a complex number class which supports most
* common operations. Though, not all of them has been tested. Some are staight
* forward implementation as you can find them on Wikipedia and other sources.
* 
* In addition the FFT class contains a fast implementation of the Fast Fourier
* Transformation. It's basically a port of the implementation of Paul Bourke
* http://paulbourke.net/miscellaneous/dft/
* 
* CalculateFFT is designed to perform both the FFT as well as the inverse FFT.
* If you pass "true" to the "reverse" parameter it will calculate the inverse
* FFT. The FFT is calculated in-place on an array of Complex values.
* 
* For convenience i added a few helper functions to convert a float array as
* well as a double array into a Complex array. The reverse also exists. The
* Complex2Float and Complex2Double have also a "reverse" parameter which will
* preserve the sign of the real part
* 
* Keep in mind when using this as FFT filter you have to preserve the Complex
* samples as the phase information might be required for the inverse FFT.
* 
* Final note: I've written this mainly to better understand the FFT algorithm.
* The original version of Paul Bourke is probably faster, but i wanted to use
* actual Complex numbers so it's easier to follow the code.
* 
* 
* Copyright (c) 2015 Bunny83
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
* 
*****/
#endregion License and Information
#define UNITY


namespace B83.MathHelpers
{
    using System;
    public struct Complex
    {
        public double real;
        public double img;
        public Complex(double aReal, double aImg)
        {
            real = aReal;
            img = aImg;
        }
        public static Complex FromAngle(double aAngle, double aMagnitude)
        {
            return new Complex(Math.Cos(aAngle) * aMagnitude, Math.Sin(aAngle) * aMagnitude);
        }

        public Complex conjugate { get { return new Complex(real, -img); } }
        public double magnitude { get { return Math.Sqrt(real * real + img * img); } }
        public double sqrMagnitude { get { return real * real + img * img; } }
        public double angle { get { return Math.Atan2(img, real); } }

        public float fReal { get { return (float)real; } set { real = value; } }
        public float fImg { get { return (float)img; } set { img = value; } }
        public float fMagnitude { get { return (float)Math.Sqrt(real * real + img * img); } }
        public float fSqrMagnitude { get { return (float)(real * real + img * img); } }
        public float fAngle { get { return (float)Math.Atan2(img, real); } }

        #region Basic operations + - * /
        public static Complex operator +(Complex a, Complex b)
        {
            return new Complex(a.real + b.real, a.img + b.img);
        }
        public static Complex operator +(Complex a, double b)
        {
            return new Complex(a.real + b, a.img);
        }
        public static Complex operator +(double b, Complex a)
        {
            return new Complex(a.real + b, a.img);
        }

        public static Complex operator -(Complex a)
        {
            return new Complex(-a.real, -a.img);
        }

        public static Complex operator -(Complex a, Complex b)
        {
            return new Complex(a.real - b.real, a.img - b.img);
        }
        public static Complex operator -(Complex a, double b)
        {
            return new Complex(a.real - b, a.img);
        }
        public static Complex operator -(double b, Complex a)
        {
            return new Complex(b - a.real, -a.img);
        }

        public static Complex operator *(Complex a, Complex b)
        {
            return new Complex(a.real * b.real - a.img * b.img, a.real * b.img + a.img * b.real);
        }
        public static Complex operator *(Complex a, double b)
        {
            return new Complex(a.real * b, a.img * b);
        }
        public static Complex operator *(double b, Complex a)
        {
            return new Complex(a.real * b, a.img * b);
        }

        public static Complex operator /(Complex a, Complex b)
        {
            double d = 1d / (b.real * b.real + b.img * b.img);
            return new Complex((a.real * b.real + a.img * b.img) * d, (-a.real * b.img + a.img * b.real) * d);
        }
        public static Complex operator /(Complex a, double b)
        {
            return new Complex(a.real / b, a.img / b);
        }
        public static Complex operator /(double a, Complex b)
        {
            double d = 1d / (b.real * b.real + b.img * b.img);
            return new Complex(a * b.real * d, -a * b.img);
        }

        #endregion Basic operations + - * /

        #region Trigonometic operations

        public static Complex Sin(Complex a)
        {
            return new Complex(Math.Sin(a.real) * Math.Cosh(a.img), Math.Cos(a.real) * Math.Sinh(a.img));
        }
        public static Complex Cos(Complex a)
        {
            return new Complex(Math.Cos(a.real) * Math.Cosh(a.img), -Math.Sin(a.real) * Math.Sinh(a.img));
        }

        private static double arcosh(double a)
        {
            return Math.Log(a + Math.Sqrt(a * a - 1));
        }
        private static double artanh(double a)
        {
            return 0.5 * Math.Log((1 + a) / (1 - a));

        }

        public static Complex ASin(Complex a)
        {
            double r2 = a.real * a.real;
            double i2 = a.img * a.img;
            double sMag = r2 + i2;
            double c = Math.Sqrt((sMag - 1) * (sMag - 1) + 4 * i2);
            double sr = a.real > 0 ? 0.5 : a.real < 0 ? -0.5 : 0;
            double si = a.img > 0 ? 0.5 : a.img < 0 ? -0.5 : 0;

            return new Complex(sr * Math.Acos(c - sMag), si * arcosh(c + sMag));
        }
        public static Complex ACos(Complex a)
        {
            return Math.PI * 0.5 - ASin(a);
        }

        public static Complex Sinh(Complex a)
        {
            return new Complex(Math.Sinh(a.real) * Math.Cos(a.img), Math.Cosh(a.real) * Math.Sin(a.img));
        }
        public static Complex Cosh(Complex a)
        {
            return new Complex(Math.Cosh(a.real) * Math.Cos(a.img), Math.Sinh(a.real) * Math.Sin(a.img));
        }
        public static Complex Tan(Complex a)
        {
            return new Complex(Math.Sin(2 * a.real) / (Math.Cos(2 * a.real) + Math.Cosh(2 * a.img)), Math.Sinh(2 * a.img) / (Math.Cos(2 * a.real) + Math.Cosh(2 * a.img)));
        }
        public static Complex ATan(Complex a)
        {
            double sMag = a.real * a.real + a.img * a.img;
            double i = 0.5 * artanh(2 * a.img / (sMag + 1));
            if (a.real == 0)
                return new Complex(a.img > 1 ? Math.PI * 0.5 : a.img < -1 ? -Math.PI * 0.5 : 0, i);
            double sr = a.real > 0 ? 0.5 : a.real < 0 ? -0.5 : 0;
            return new Complex(0.5 * (Math.Atan((sMag - 1) / (2 * a.real)) + Math.PI * sr), i);
        }

        #endregion Trigonometic operations

        public static Complex Exp(Complex a)
        {
            double e = Math.Exp(a.real);
            return new Complex(e * Math.Cos(a.img), e * Math.Sin(a.img));
        }
        public static Complex Log(Complex a)
        {
            return new Complex(Math.Log(Math.Sqrt(a.real * a.real + a.img * a.img)), Math.Atan2(a.img, a.real));
        }
        public Complex sqrt(Complex a)
        {
            double r = Math.Sqrt(Math.Sqrt(a.real * a.real + a.img * a.img));
            double halfAngle = 0.5 * Math.Atan2(a.img, a.real);
            return new Complex(r * Math.Cos(halfAngle), r * Math.Sin(halfAngle));
        }

#if UNITY
        public static explicit operator UnityEngine.Vector2(Complex a)
        {
            return new UnityEngine.Vector2((float)a.real, (float)a.img);
        }
        public static explicit operator UnityEngine.Vector3(Complex a)
        {
            return new UnityEngine.Vector3((float)a.real, (float)a.img);
        }
        public static explicit operator UnityEngine.Vector4(Complex a)
        {
            return new UnityEngine.Vector4((float)a.real, (float)a.img);
        }
        public static implicit operator Complex(UnityEngine.Vector2 a)
        {
            return new Complex(a.x, a.y);
        }
        public static implicit operator Complex(UnityEngine.Vector3 a)
        {
            return new Complex(a.x, a.y);
        }
        public static implicit operator Complex(UnityEngine.Vector4 a)
        {
            return new Complex(a.x, a.y);
        }
#endif
    }

    public class FFT
    {
        // aSamples.Length need to be a power of two
        public static Complex[] CalculateFFT(Complex[] aSamples, bool aReverse)
        {
            int power = (int)Math.Log(aSamples.Length, 2);
            int count = 1;
            for (int i = 0; i < power; i++)
                count <<= 1;

            int mid = count >> 1; // mid = count / 2;
            int j = 0;
            for (int i = 0; i < count - 1; i++)
            {
                if (i < j)
                {
                    var tmp = aSamples[i];
                    aSamples[i] = aSamples[j];
                    aSamples[j] = tmp;
                }
                int k = mid;
                while (k <= j)
                {
                    j -= k;
                    k >>= 1;
                }
                j += k;
            }
            Complex r = new Complex(-1, 0);
            int l2 = 1;
            for (int l = 0; l < power; l++)
            {
                int l1 = l2;
                l2 <<= 1;
                Complex r2 = new Complex(1, 0);
                for (int n = 0; n < l1; n++)
                {
                    for (int i = n; i < count; i += l2)
                    {
                        int i1 = i + l1;
                        Complex tmp = r2 * aSamples[i1];
                        aSamples[i1] = aSamples[i] - tmp;
                        aSamples[i] += tmp;
                    }
                    r2 = r2 * r;
                }
                r.img = Math.Sqrt((1d - r.real) / 2d);
                if (!aReverse)
                    r.img = -r.img;
                r.real = Math.Sqrt((1d + r.real) / 2d);
            }
            if (!aReverse)
            {
                double scale = 1d / count;
                for (int i = 0; i < count; i++)
                    aSamples[i] *= scale;
            }
            return aSamples;
        }


        #region float / double array conversion helpers
        public static Complex[] Double2Complex(double[] aData)
        {
            Complex[] data = new Complex[aData.Length];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new Complex(aData[i], 0);
            }
            return data;
        }
        public static double[] Complex2Double(Complex[] aData, bool aReverse)
        {
            double[] result = new double[aData.Length];
            if (!aReverse)
            {
                for (int i = 0; i < aData.Length; i++)
                {
                    result[i] = aData[i].magnitude;
                }
                return result;
            }
            for (int i = 0; i < aData.Length; i++)
            {
                result[i] = Math.Sign(aData[i].real) * aData[i].magnitude;
            }
            return result;
        }

        public static Complex[] Float2Complex(float[] aData)
        {
            Complex[] data = new Complex[aData.Length];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new Complex(aData[i], 0);
            }
            return data;
        }
        public static float[] Complex2Float(Complex[] aData, bool aReverse)
        {
            float[] result = new float[aData.Length];
            if (!aReverse)
            {
                for (int i = 0; i < aData.Length; i++)
                {
                    result[i] = (float)aData[i].magnitude;
                }
                return result;
            }
            for (int i = 0; i < aData.Length; i++)
            {
                result[i] = (float)(Math.Sign(aData[i].real) * aData[i].magnitude);
            }
            return result;
        }
        #endregion float / double array conversion helpers
    }
}