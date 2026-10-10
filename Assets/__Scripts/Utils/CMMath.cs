using System;
using System.Collections.Generic;
using System.Linq;

public static class CMMath
{
    // Scroll tweaks accumulate binary float error (seven +0.1 steps land on 0.700000048), which then
    // leaks into customData. Snap committed node values to the decimal grid the step actually targets.
    // Scroll tweaks are never super high precision so rounding to 3 decimal places is always fine.
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
