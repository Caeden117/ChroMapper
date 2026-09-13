using System.Collections.Generic;

public static class IntExtensions
{
    public static IEnumerable<int> GetBitIndex(this int num)
    {
        for (var i = 0; i < 32; i++)
            if ((num & (1 << i)) != 0)
                yield return i;
    }
}
