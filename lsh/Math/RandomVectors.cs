using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace lsh.LshMath;

public static class RandomVectors
{
    public static Vector<double> RandomNormal(int dimensions, Random rand)
    {
        double[] values = new double[dimensions];
        for (int i = 0; i < dimensions; ++i)
            values[i] = rand.NextGaussian();
        return DenseVector.Build.DenseOfArray(values);
    }
    public static Vector<double> RandomGivenLength(int dimensions, double length, Random rand)
    {
        while (true)
        {
            var v = RandomNormal(dimensions, rand);
            double l = v.L2Norm();
            if (l > 0)
                return v * (length / l);
        }
    }

    public static Vector<double> RandomUniform(int dimensions, double min, double max, Random rand)
    {
        double[] values = new double[dimensions];
        for (int i = 0; i < dimensions; ++i)
            values[i] = rand.NextDouble() * (max - min) + min;
        return DenseVector.Build.DenseOfArray(values);
    }
}

