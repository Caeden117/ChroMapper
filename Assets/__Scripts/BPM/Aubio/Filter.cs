using System;
using System.Diagnostics;

namespace Aubio
{
    /* Must use double. float will NOT give reliable results */
    public class Filter
    {
        public int Order { get; private set; }
        public int SampleRate { get; set; }
        public double[] Feedback { get; private set; } //a
        public double[] Forward { get; private set; } //b
        private readonly double[] y;
        private readonly double[] x;

        public Filter(int order)
        {
            Debug.Assert(order != 0);

            x = new double[order];
            y = new double[order];
            Feedback = new double[order];
            Forward = new double[order];

            /* by default, sample rate is not set */
            SampleRate = 0;
            Order = order;

            /* set default to identity */
            Feedback[0] = 1.0;
            Forward[0] = 1.0;
        }

        public void DoOutplace(double[] input, double[] output)
        {
            Array.Copy(input, output, output.Length);
            Do(output);
        }

        public void Do(double[] input)
        {
            int j, l;
            for (j = 0; j < input.Length; j++)
            {
                /* new input */
                x[0] = Utils.KillDenormal(input[j]);
                y[0] = Forward[0] * x[0];
                for (l = 1; l < Order; l++)
                {
                    y[0] += Forward[l] * x[l];
                    y[0] -= Feedback[l] * y[l];
                }
                /* new output */
                input[j] = y[0];
                /* store for next sample */
                for (l = Order - 1; l > 0; l--)
                {
                    x[l] = x[l - 1];
                    y[l] = y[l - 1];
                }
            }
        }

        // runs the filter twice, forward and backward, to
        // compensate the phase shifting of the forward operation.
        /* The rough way: reset memory of filter between each run to avoid end effects. */
        public void DoFiltFilt(double[] input, double[] tmp)
        {
            int j;
            var length = input.Length;
            /* apply filtering */
            Do(input);
            DoReset();
            /* mirror */
            for (j = 0; j < length; j++)
                tmp[length - j - 1] = input[j];
            /* apply filtering on mirrored */
            Do(tmp);
            DoReset();
            /* invert back */
            for (j = 0; j < length; j++)
                input[j] = tmp[length - j - 1];
        }

        public void DoReset()
        {
            Array.Clear(x, 0, x.Length);
            Array.Clear(y, 0, y.Length);
        }
    }
}
