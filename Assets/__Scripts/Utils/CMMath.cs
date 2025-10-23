using System.Collections.Generic;
using System.Linq;

public static class CMMath
{
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
