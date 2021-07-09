using System;
using System.Collections.Generic;
using System.Linq;
using KeepCoding;

public static class Extensions
{
    public static bool IsSuccessive(this IEnumerable<int> nums)
    {
        return nums.Pairwise((x, y) => y - x).Distinct().Count() == 1;
    }

    public static IEnumerable<T> Pairwise<T>(this IEnumerable<T> source, Func<T, T, T> selector)
    {
        if (source == null) throw new ArgumentNullException("source");
        if (selector == null) throw new ArgumentNullException("selector");

        using (var e = source.GetEnumerator())
        {
            if (!e.MoveNext()) throw new InvalidOperationException("Sequence cannot be empty.");

            T prev = e.Current;

            if (!e.MoveNext()) throw new InvalidOperationException("Sequence must contain at least two elements.");

            do
            {
                yield return selector(prev, e.Current);
                prev = e.Current;
            } while (e.MoveNext());
        }
    }

    public static int ToIntViaA1Z26(this char c)
    {
        var index = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(c.ToUpper());
        return index != -1 ? (index + 1) : (c - '0');
    }
}