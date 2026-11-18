using System;
using System.Collections.Generic;
using System.Linq;

public static class CMMath
{
    // Round scroll edits to three decimal places so repeated steps such as +0.1 do not save floating-point
    // noise in customData.
    public static float RoundToDecimals(float value, int decimals = 3) =>
        (float)Math.Round(value, decimals);

    public static int GetLowestDenominator(int a)
    {
        if (a <= 1) return 2;
        IEnumerable<int> factors = PrimeFactors(a);
        return factors.Any() ? factors.Max() : a;
    }

    public static List<int> PrimeFactors(int a)
    {
        var retval = new List<int>();
        for (var b = 2; a > 1; b++)
        {
            while (a % b == 0)
            {
                a /= b;
                retval.Add(b);
            }
        }

        return retval;
    }
}
