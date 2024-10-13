using System.Net.Http.Headers;

namespace lsh.LshMath;

public static class MathExtensions
{
    public static double NextGaussian(this Random rand)
    {
        // using Box-Muller transform
        double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
        double u2 = 1.0 - rand.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) *
                     Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
    }

    public static double NextGaussian(this Random rand, double mean, double stdDev)
    {
        return rand.NextGaussian() * stdDev + mean;
    }

    public static long GetSequenceHashCode<T>(this IEnumerable<T> sequence)
    {
        unchecked
        {
            return sequence.Aggregate(487L, (current, item) =>
                (current * 31) + (item?.GetHashCode() ?? 0));
        }
    }

    public static TValue ComputeIfAbsent<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> valueFactory)
    where TKey : notnull
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value;
        }
        else
        {
            value = valueFactory();
            dict[key] = value;
            return value;
        }
    }

    public static void ForEach<T>(this IEnumerable<T> sequence, Action<T, int> action)
    {
        var index = 0;
        foreach (var item in sequence)
        {
            action(item, index++);
        }
    }
}
