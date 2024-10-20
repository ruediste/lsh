using System.Diagnostics;
using lsh;
using lsh.LshMath;
using MathNet.Numerics;
using ScottPlot;

namespace tests;

public class Tests
{

    [OneTimeSetUp]
    public void Setup()
    {
        // Control.NativeProviderPath = ".";
        // Control.UseNativeMKL();
    }

    [Test]
    public void OverallAlgo()
    {
        Console.WriteLine($"start");
        // build test distribution
        var d = 100;
        var N = 20000;
        var random = new Random(0);

        var data = Enumerable.Range(0, N).SelectMany(_ =>
        {
            var v = RandomVectors.RandomUniform(d, 0, 20, random);
            return new List<Vector<double>> {
                v,
                v.Add(RandomVectors.RandomUniform(d,0,1,random) ),
            };
        }).ToArray();

        var watch = new Stopwatch();
        watch.Start();
        var dNn = ProbabilityDensityFunction.FromNnDistances(d, data, random);
        Console.WriteLine($"build nn dist: {watch.ElapsedMilliseconds} ms");

        watch.Restart();
        var dAny = ProbabilityDensityFunction.FromAnyDistances(d, data, random);
        Console.WriteLine($"build any dist: {watch.ElapsedMilliseconds} ms");

        dAny.Plot("dAny.png");
        dNn.Plot("dNn.png");

        var set = new LshSet<int>(data.Length, d, 0.1, dNn, dAny);
        Console.WriteLine(set);

        // testing the algorithm
        watch.Restart();
        data.ForEach((v, i) => set.Add(v, i));
        Console.WriteLine($"Index Data: {watch.ElapsedMilliseconds} ms");
        new HistogramPDF(set.BucketSizes.Select(x => (double)x)).Plot("bucketSizes.png");

        watch.Restart();
        List<double> distances = new();
        foreach (var v in data)
        {
            var q = v.Clone();
            q[0] += random.NextDouble() + 1;
            var nn = set.Query(q);
            if (nn != null)
            {
                var dist = (nn.Value.P - q).L2Norm();
                distances.Add(dist);
            }
        }
        Console.WriteLine($"Query Data: {watch.ElapsedMilliseconds} ms");
        new HistogramPDF(distances).Plot("foundNN.png");
    }

    [Test]
    public void UnitImpulseMultiplication()
    {
        var random = new Random(0);
        // calculate on a sample data set
        var data = Enumerable.Range(0, 2000).Select(_ => 5 * random.NextGaussian()).ToArray();
        var dataHist = new HistogramPDF(data);

        // calculate theoretically
        UnitImpulsePdf u = new() { Value = 5 };
        var multiplied = u.MultiplyWithUnitNormal();

        Plot plot = new();
        dataHist.Plot(plot);
        plot.Add.Function(x => multiplied.Probability(x));
        plot.Axes.SetLimitsX(-20, 20);
        plot.Axes.SetLimitsY(0, 0.2);
        plot.SavePng("unitImpulseMultiplication.png", 800, 600);
    }

    [Test]
    public void HistogramMultiplication()
    {
        var random = new Random(0);

        // calculate on a sample data set
        var xValues = Enumerable.Range(0, 2000).Select(_ => random.NextGaussian() * 0.1 + 4)
            // .Concat(Enumerable.Range(0, 2000).Select(_ => random.NextGaussian() * 2))
            // .Concat(Enumerable.Range(0, 2000).Select(_ => random.NextDouble() - 3))
            .ToArray();
        var xValuesHist = new HistogramPDF(xValues);
        Plot plot = new();
        xValuesHist.Plot(plot);
        new GaussianPDF { Mean = 4, StandardDeviation = 0.1 }.Plot(plot);
        plot.SavePng("histogramMultiplicationXValues.png", 400, 300);

        var data = xValues.Select(x => x * random.NextGaussian()).Where(x => x > -15 && x < 15).ToArray();
        var dataHist = new HistogramPDF(data);

        // calculate theoretically
        var multiplied = dataHist.MultiplyWithUnitNormal();

        plot = new();
        dataHist.Plot(plot);
        plot.Add.Function(x => multiplied.Probability(x));
        plot.Axes.SetLimitsX(-20, 20);
        plot.Axes.SetLimitsY(0, 0.3);
        plot.SavePng("histogramMultiplication.png", 800, 600);
    }
}