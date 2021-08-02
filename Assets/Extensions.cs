using System;
using System.Collections.Generic;
using System.Linq;
using KeepCoding;

public static class Extensions
{
    public static bool IsStraight(this IEnumerable<int> nums)
    {
        var differences = nums.Pairwise((x, y) => y - x).Distinct().ToList();
        return differences.Count == 1 && differences[0] == 1;
    }

    public static IEnumerable<TResult> Pairwise<T, TResult>(this IEnumerable<T> source, Func<T, T, TResult> selector)
    {
        if (source == null) throw new ArgumentNullException("source");
        if (selector == null) throw new ArgumentNullException("selector");

        using (var e = source.GetEnumerator())
        {
            if (!e.MoveNext()) throw new InvalidOperationException("Sequence cannot be empty.");

            var prev = e.Current;

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