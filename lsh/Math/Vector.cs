namespace lsh.LshMath;

public class DenseVector
{
    public required double[] Values { get; init; }

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

    public double Dot(DenseVector other) => Values.Zip(other.Values, (a, b) => a * b).Sum();
}

