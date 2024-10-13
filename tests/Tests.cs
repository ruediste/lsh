using System.Security.Cryptography;
using lsh.LshMath;
using ScottPlot;
using ScottPlot.DataSources;

namespace tests;

public class Tests
{

    [Test]
    public void OverallAlgo()
    {
        Console.WriteLine($"start");
        // build test distribution
        var d = 20;
        var N = 200;
        var random = new Random(0);

        var data = Enumerable.Range(0, N / 4).SelectMany(_ =>
        {
            var v = DenseVector.RandomUniform(d, 0, 10, random);
            return new List<DenseVector> {
                v,
                v.Add(DenseVector.RandomUniform(d,0,1,random) ),
                v.Add(DenseVector.RandomUniform(d,0,1,random) ),
                v.Add(DenseVector.RandomUniform(d,0,1,random) )
            };
        }).ToArray();

        var (dNn, dAny) = ProbabilityDensityFunction.FromDistances(data);

        dAny.Plot("dAny.png");
        dNn.Plot("dNn.png");

        Console.WriteLine($"plotted");
        var gNn = MultiplicationPDF.CreateUnitNormal(dNn);
        var gAny = MultiplicationPDF.CreateUnitNormal(dAny);

        Console.WriteLine($"gNn: {gNn}");
        Console.WriteLine($"gAny: {gAny}");

        Console.WriteLine($"Triangle: {gNn.TriangleProbability(1)}");

        Plot plot = new();
        Func<double, double> f = x => Math.Log(gAny.TriangleProbability(x)) / Math.Log(gNn.TriangleProbability(x));
        plot.Add.Function(f);
        plot.Axes.SetLimitsX(1, 200);
        plot.Axes.SetLimitsY(0, 5);
        plot.SavePng("f.png", 800, 600);
        var w = FindArgMax.Compute(f, 1e-3);
        Console.WriteLine($"w = {w}");
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
        var multiplied = MultiplicationPDF.CreateUnitNormal(u);

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
        var xValues = Enumerable.Range(0, 2000).Select(_ => random.NextGaussian() + 4)
            .Concat(Enumerable.Range(0, 2000).Select(_ => random.NextGaussian() * 2))
            .Concat(Enumerable.Range(0, 2000).Select(_ => random.NextDouble() - 3)).ToArray();
        var xValuesHist = new HistogramPDF(xValues);
        xValuesHist.Plot("histogramMultiplicationXValues.png");

        var data = xValues.Select(x => x * random.NextGaussian()).Where(x => x > -10 && x < 10).ToArray();
        var dataHist = new HistogramPDF(data);
        Console.WriteLine($"{dataHist.Bins.Length} {data.Length} {xValues.Length}");

        // calculate theoretically
        var multiplied = MultiplicationPDF.CreateUnitNormal(dataHist);

        Plot plot = new();
        dataHist.Plot(plot);
        plot.Add.Function(x => multiplied.Probability(x));
        plot.Axes.SetLimitsX(-20, 20);
        plot.Axes.SetLimitsY(0, 0.3);
        plot.SavePng("histogramMultiplication.png", 800, 600);
    }
}