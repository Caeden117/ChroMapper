using UnityEngine;

public static class WindowCoefficients
{
    public static float[] GetWindowForSize(int sampleSize)
        => SineExpansion(sampleSize, 0.35875f, -0.48829f, 0.14128f, -0.01168f);

    public static float Signal(float[] window) {
        var signal = 0.0f;
        for (var i = 0; i < window.Length; i++) {
            signal += window[i];
        }

        return 1.0f / (signal / window.Length);
    }

    private static float[] SineExpansion(int points, params float[] coefficients) {
        var window = new float[points];

        for (var i = 0; i < points; i++) {
            var z = 2.0f * Mathf.PI * i / points;

            var coeff = coefficients[0];
            for (var j = 1; j < coefficients.Length; j++) {
                coeff += coefficients[j] * Mathf.Cos(j * z); 
            }
            window[i] = coeff;
        }

        return window;
    }
}
