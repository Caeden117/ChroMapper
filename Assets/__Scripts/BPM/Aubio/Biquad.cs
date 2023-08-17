namespace Aubio
{
    public class Biquad : Filter
    {
        // b - forward filter coefficients, a - feedback coefficients
        public Biquad(double b0, double b1, double b2, double a1, double a2) : base(3)
        {
            SetCoefficients(b0, b1, b2, a1, a2);
        }

        //aubio_filter_set_biquad
        public void SetCoefficients(double b0, double b1, double b2, double a1, double a2)
        {
            //if (order != 3)
            //{
            //    AUBIO_ERROR("order of biquad filter must be 3, not %d\n", order);
            //    return AUBIO_FAIL;
            //}
            Forward[0] = b0;
            Forward[1] = b1;
            Forward[2] = b2;
            Feedback[0] = 1.0;
            Feedback[1] = a1;
            Feedback[2] = a2;
        }
    }
}
