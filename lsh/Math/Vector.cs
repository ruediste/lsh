namespace lsh.LshMath;


public interface Vector
{
    public IEnumerable<int> Indices { get; }

    public IEnumerable<double> Values { get; }
}

public class DenseVector : Vector
{
    public required double[] Values { get; init; }

    public IEnumerable<int> Indices => Enumerable.Range(0, Values.Length);

    IEnumerable<double> Vector.Values => Values;

    public static DenseVector RandomNormal(int length, Random rand)
    {
        double[] values = new double[length];
        for (int i = 0; i < length; ++i)
            values[i] = rand.NextGaussian();
        return new DenseVector { Values = values };
    }
    public static DenseVector RandomUniform(int length, double min, double max, Random rand)
    {
        double[] values = new double[length];
        for (int i = 0; i < length; ++i)
            values[i] = rand.NextDouble() * (max - min) + min;
        return new DenseVector { Values = values };
    }

    public double DistanceTo(DenseVector other)
    {
        double sum = 0;
        for (int i = 0; i < Values.Length && i < other.Values.Length; ++i)
            sum += Math.Pow(Values[i] - other.Values[i], 2);
        return Math.Sqrt(sum);
    }

    public DenseVector Add(DenseVector other)
    {
        double[] values = new double[Values.Length];
        for (int i = 0; i < Values.Length && i < other.Values.Length; ++i)
            values[i] = Values[i] + other.Values[i];
        return new DenseVector { Values = values };
    }

    public DenseVector Add(Vector other)
    {
        double[] values = new double[Values.Length];
        foreach ((double value, int index) in other.Values.Zip(other.Indices))
        {
            values[index] += value;
        }
        return new DenseVector { Values = values };
    }
}

public class SparseVector : Vector
{
    public required int[] Indices { get; init; }
    public required double[] Values { get; init; }

    IEnumerable<int> Vector.Indices => Indices;

    IEnumerable<double> Vector.Values => Values;
}
