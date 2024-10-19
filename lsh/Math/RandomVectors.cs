using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace lsh.LshMath;

public static class RandomVectors
{
    public static Vector<double> RandomNormal(int length, Random rand)
    {
        double[] values = new double[length];
        for (int i = 0; i < length; ++i)
            values[i] = rand.NextGaussian();
        return DenseVector.Build.DenseOfArray(values);
    }
    public static Vector<double> RandomUniform(int length, double min, double max, Random rand)
    {
        double[] values = new double[length];
        for (int i = 0; i < length; ++i)
            values[i] = rand.NextDouble() * (max - min) + min;
        return DenseVector.Build.DenseOfArray(values);
    }
}

