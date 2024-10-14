using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using lsh;
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
        var N = 2000;
        var random = new Random(0);

        var data = Enumerable.Range(0, N).SelectMany(_ =>
        {
            var v = DenseVector.RandomUniform(d, 0, 10, random);
            return new List<DenseVector> {
                v,
                v.Add(DenseVector.RandomUniform(d,0,1,random) ),
            };
        }).ToArray();

        var dNn = ProbabilityDensityFunction.FromNnDistances(data, random);
        var dAny = ProbabilityDensityFunction.FromAnyDistances(data, random);

        dAny.Plot("dAny.png");
        dNn.Plot("dNn.png");

        var set = new LshSet<int>(data.Length, d, 0.1, /*dNn*/ new UnitImpulsePdf() { Value = 2 }, dAny);
        Console.WriteLine(set);

        // testing the algorithm
        data.ForEach((v, i) => set.Add(v, i));

        List<double> distances = new();
        foreach (var v in data)
        {
            var values = v.Values.ToArray();
            values[0] += random.NextDouble() + 1;
            var q = new DenseVector() { Values = values };
            var nn = set.Query(q);
            if (nn != null)
            {
                var dist = nn.Value.P.DistanceTo(q);
                distances.Add(dist);
            }
        }
        Console.WriteLine($"{distances.Count}");
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
        var xValues = Enumerable.Range(0, 2000).Select(_ => random.NextGaussian() + 4)
            .Concat(Enumerable.Range(0, 2000).Select(_ => random.NextGaussian() * 2))
            .Concat(Enumerable.Range(0, 2000).Select(_ => random.NextDouble() - 3)).ToArray();
        var xValuesHist = new HistogramPDF(xValues);
        xValuesHist.Plot("histogramMultiplicationXValues.png");

        var data = xValues.Select(x => x * random.NextGaussian()).Where(x => x > -10 && x < 10).ToArray();
        var dataHist = new HistogramPDF(data);
        Console.WriteLine($"{dataHist.Bins.Length} {data.Length} {xValues.Length}");

        // calculate theoretically
        var multiplied = dataHist.MultiplyWithUnitNormal();

        Plot plot = new();
        dataHist.Plot(plot);
        plot.Add.Function(x => multiplied.Probability(x));
        plot.Axes.SetLimitsX(-20, 20);
        plot.Axes.SetLimitsY(0, 0.3);
        plot.SavePng("histogramMultiplication.png", 800, 600);
    }
}