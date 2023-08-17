using System;
namespace Aubio
{
    // Based on aubio's cvec
    public struct Polar
    {
        public double Norm;
        public double Phase;

        public Polar(double norm, double phase)
        {
            Norm = norm; Phase = phase;
            //Norm = norm * norm; Phase = phase;
        }

        public void LogMag(double lambda) => Norm = Math.Log((lambda * Norm) + 1);
    }
}
