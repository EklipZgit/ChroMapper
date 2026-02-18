using System.Collections.Generic;

public static class IntExtensions
{
    public static List<int> Get1BitPositions(this int num)
    {
        var list = new List<int>();
        for (var i = 0; i < 32; i++)
        {
            if ((num & (1 << i)) != 0) list.Add(i);
        }

        return list;
    }
}
