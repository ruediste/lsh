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
}
