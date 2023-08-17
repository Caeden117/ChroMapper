using System;

namespace Aubio
{
    /// <summary>
    /// Spectral description functions used in onset detection
    /// from aubio's specdesc.c but only a couple of the methods implemented
    /// </summary>
    public class SpectralDescription
    {

        /** onsetdetection types */
        public enum OnsetType
        {
            //Energy,    /**< energy based */
            //SpecDiff,  /**< spectral diff */
            HFC,       /**< high frequency content **the "old default"*/
            ComplexDomain,   /**< complex domain */
            //Phase,     /**< phase fast */
            //WPhase,    /**< weighted phase */
            KL,        /**< Kullback Liebler */
            //MKL,       /**< modified Kullback Liebler */
            SpectralFlux,  /**< spectral flux */
            //Centroid,  /**< spectral centroid */
            //Spread,    /**< spectral spread */
            //Skewness,  /**< spectral skewness */
            //Kurtosis,  /**< spectral kurtosis */
            //Slope,     /**< spectral slope */
            //Decrease,  /**< spectral decrease */
            //Rolloff,   /**< spectral rolloff */
        }


        private readonly OnsetType onsetType; /**< onset detection type */
        /** Pointer to aubio_specdesc_<type> function */
        private readonly Action<Polar[], double[]> method;
        //private readonly double threshold;      /**< minimum norm threshold for phase and specdiff */
        private readonly double[] oldMag;        /**< previous norm vector */
        private readonly double[] dev1;         /**< current onset detection measure vector */
        private readonly double[] theta1;        /**< previous phase vector, one frame behind */
        private readonly double[] theta2;        /**< previous phase vector, two frames behind */
        // The methods implemented do not require a histogram, leaving for reference
        //aubio_hist_t* histog; /**< histogram */


        public SpectralDescription(OnsetType type, int size)
        {
            var rSize = (size / 2) + 1;
            onsetType = type;
            switch (onsetType)
            {
                /* for both energy and hfc, only fftgrain->norm is required */
                //case aubio_onset_energy:
                //    break;
                case OnsetType.HFC:
                    break;
                /* the other approaches will need some more memory spaces */
                case OnsetType.ComplexDomain:
                    oldMag = new double[rSize];
                    dev1 = new double[rSize];
                    theta1 = new double[rSize];
                    theta2 = new double[rSize];
                    break;
                //case aubio_onset_phase:
                //case aubio_onset_wphase:
                //    o->dev1 = new_fvec(rsize);
                //    o->theta1 = new_fvec(rsize);
                //    o->theta2 = new_fvec(rsize);
                //    o->histog = new_aubio_hist(0.0, PI, 10);
                //    o->threshold = 0.1;
                //    break;
                //case aubio_onset_specdiff:
                //    o->oldmag = new_fvec(rsize);
                //    o->dev1 = new_fvec(rsize);
                //    o->histog = new_aubio_hist(0.0, PI, 10);
                //    o->threshold = 0.1;
                //    break;
                case OnsetType.KL:
                //case aubio_onset_mkl:
                case OnsetType.SpectralFlux:
                    oldMag = new double[rSize];
                    break;
                default:
                    break;
            }

            switch (onsetType)
            {
                //case aubio_onset_energy:
                //    o->funcpointer = aubio_specdesc_energy;
                //    break;
                case OnsetType.HFC:
                    method = HFC;
                    break;
                case OnsetType.ComplexDomain:
                    method = Complex;
                    break;
                //case aubio_onset_phase:
                //    o->funcpointer = aubio_specdesc_phase;
                //    break;
                //case aubio_onset_wphase:
                //    o->funcpointer = aubio_specdesc_wphase;
                //    break;
                //case aubio_onset_specdiff:
                //    o->funcpointer = aubio_specdesc_specdiff;
                //    break;
                case OnsetType.KL:
                    method = KL;
                    break;
                //case aubio_onset_mkl:
                //    o->funcpointer = aubio_specdesc_mkl;
                //    break;
                case OnsetType.SpectralFlux:
                    method = SpecFlux;
                    break;
                //case aubio_specmethod_centroid:
                //    o->funcpointer = aubio_specdesc_centroid;
                //    break;
                //case aubio_specmethod_spread:
                //    o->funcpointer = aubio_specdesc_spread;
                //    break;
                //case aubio_specmethod_skewness:
                //    o->funcpointer = aubio_specdesc_skewness;
                //    break;
                //case aubio_specmethod_kurtosis:
                //    o->funcpointer = aubio_specdesc_kurtosis;
                //    break;
                //case aubio_specmethod_slope:
                //    o->funcpointer = aubio_specdesc_slope;
                //    break;
                //case aubio_specmethod_decrease:
                //    o->funcpointer = aubio_specdesc_decrease;
                //    break;
                //case aubio_specmethod_rolloff:
                //    o->funcpointer = aubio_specdesc_rolloff;
                //    break;
                default:
                    break;
            }
        }

        public void Do(Polar[] fftGrain, double[] onset) => method.Invoke(fftGrain, onset);

        /* Energy based onset detection function */
        //void aubio_specdesc_energy(aubio_specdesc_t* o UNUSED,
        //    const cvec_t* fftgrain, fvec_t * onset) {
        //  uint_t j;
        //        onset->data[0] = 0.;
        //  for (j=0;j<fftgrain->length;j++) {
        //    onset->data[0] += SQR(fftgrain->norm[j]);
        //    }
        //}

        /* High Frequency Content onset detection function */
        private void HFC(Polar[] fftGrain, double[] onset)
        {
            onset[0] = 0.0;
            for (var j = 0; j < fftGrain.Length; j++)
            {
                onset[0] += (j + 1) * fftGrain[j].Norm;
            }
        }

        /* Complex Domain Method onset detection function */
        private void Complex(Polar[] fftGrain, double[] onset)
        {
            var nbins = fftGrain.Length;
            onset[0] = 0.0;
            for (var j = 0; j < nbins; j++)
            {
                // compute the predicted phase
                dev1[j] = (2.0 * theta1[j]) - theta2[j];
                // compute the euclidean distance in the complex domain
                // sqrt ( r_1^2 + r_2^2 - 2 * r_1 * r_2 * \cos ( \phi_1 - \phi_2 ) )
                onset[0] += Math.Sqrt(Math.Abs((oldMag[j] * oldMag[j]) + (fftGrain[j].Norm * fftGrain[j].Norm)
                        - (2 * oldMag[j] * fftGrain[j].Norm
                        * Math.Cos(dev1[j] - fftGrain[j].Phase))));
                /* swap old phase data (need to remember 2 frames behind)*/
                theta2[j] = theta1[j];
                theta1[j] = fftGrain[j].Phase;
                /* swap old magnitude data (1 frame is enough) */
                oldMag[j] = fftGrain[j].Norm;
            }
        }

        /* Phase Based Method onset detection function */
        //void aubio_specdesc_phase(aubio_specdesc_t* o,
        //    const cvec_t* fftgrain, fvec_t* onset)
        //{
        //    uint_t j;
        //    uint_t nbins = fftgrain->length;
        //    onset->data[0] = 0.0;
        //    o->dev1->data[0] = 0.;
        //    for (j = 0; j < nbins; j++)
        //    {
        //        o->dev1->data[j] =
        //          aubio_unwrap2pi(
        //              fftgrain->phas[j]
        //              - 2.0 * o->theta1->data[j]
        //              + o->theta2->data[j]);
        //        if (o->threshold < fftgrain->norm[j])
        //            o->dev1->data[j] = ABS(o->dev1->data[j]);
        //        else
        //            o->dev1->data[j] = 0.0;
        //        /* keep a track of the past frames */
        //        o->theta2->data[j] = o->theta1->data[j];
        //        o->theta1->data[j] = fftgrain->phas[j];
        //    }
        //    /* apply o->histogram */
        //    aubio_hist_dyn_notnull(o->histog, o->dev1);
        //    /* weight it */
        //    aubio_hist_weight(o->histog);
        //    /* its mean is the result */
        //    onset->data[0] = aubio_hist_mean(o->histog);
        //    //onset->data[0] = fvec_mean(o->dev1);
        //}

        /* weighted phase */
        //void
        //aubio_specdesc_wphase(aubio_specdesc_t* o,
        //    const cvec_t* fftgrain, fvec_t* onset)
        //{
        //    uint_t i;
        //    aubio_specdesc_phase(o, fftgrain, onset);
        //    for (i = 0; i < fftgrain->length; i++)
        //    {
        //        o->dev1->data[i] *= fftgrain->norm[i];
        //    }
        //    /* apply o->histogram */
        //    aubio_hist_dyn_notnull(o->histog, o->dev1);
        //    /* weight it */
        //    aubio_hist_weight(o->histog);
        //    /* its mean is the result */
        //    onset->data[0] = aubio_hist_mean(o->histog);
        //}

        /* Spectral difference method onset detection function */
        //void aubio_specdesc_specdiff(aubio_specdesc_t* o,
        //    const cvec_t* fftgrain, fvec_t* onset)
        //{
        //    uint_t j;
        //    uint_t nbins = fftgrain->length;
        //    onset->data[0] = 0.0;
        //    for (j = 0; j < nbins; j++)
        //    {
        //        o->dev1->data[j] = SQRT(
        //            ABS(SQR(fftgrain->norm[j])
        //              - SQR(o->oldmag->data[j])));
        //        if (o->threshold < fftgrain->norm[j])
        //            o->dev1->data[j] = ABS(o->dev1->data[j]);
        //        else
        //            o->dev1->data[j] = 0.0;
        //        o->oldmag->data[j] = fftgrain->norm[j];
        //    }

        //    /* apply o->histogram (act somewhat as a low pass on the
        //     * overall function)*/
        //    aubio_hist_dyn_notnull(o->histog, o->dev1);
        //    /* weight it */
        //    aubio_hist_weight(o->histog);
        //    /* its mean is the result */
        //    onset->data[0] = aubio_hist_mean(o->histog);
        //}

        /* Kullback Liebler onset detection function
         * note we use ln(1+Xn/(Xn-1+0.0001)) to avoid 
         * negative (1.+) and infinite values (+1.e-10) */
        private void KL(Polar[] fftGrain, double[] onset)
        {
            onset[0] = 0.0;
            for (var j = 0; j < fftGrain.Length; j++)
            {
                onset[0] += fftGrain[j].Norm * Math.Log(1.0 + (fftGrain[j].Norm / (oldMag[j] + 1e-1)));
                oldMag[j] = fftGrain[j].Norm;
            }
            if (double.IsNaN(onset[0])) onset[0] = 0.0;
        }

        /* Modified Kullback Liebler onset detection function
         * note we use ln(1+Xn/(Xn-1+0.0001)) to avoid 
         * negative (1.+) and infinite values (+1.e-10) */
        //void aubio_specdesc_mkl(aubio_specdesc_t* o, const cvec_t* fftgrain, fvec_t* onset)
        //{
        //    uint_t j;
        //    onset->data[0] = 0.;
        //    for (j = 0; j < fftgrain->length; j++)
        //    {
        //        onset->data[0] += LOG(1.+ fftgrain->norm[j] / (o->oldmag->data[j] + 1.e - 1));
        //        o->oldmag->data[j] = fftgrain->norm[j];
        //    }
        //    if (isnan(onset->data[0])) onset->data[0] = 0.;
        //}

        /* Spectral flux */
        private void SpecFlux(Polar[] fftGrain, double[] onset)
        {
            onset[0] = 0.0;
            for (var j = 0; j < fftGrain.Length; j++)
            {
                if (fftGrain[j].Norm > oldMag[j])
                    onset[0] += fftGrain[j].Norm - oldMag[j];
                oldMag[j] = fftGrain[j].Norm;
            }
        }
    }
}
