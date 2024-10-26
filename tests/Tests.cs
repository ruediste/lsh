using System.Diagnostics;
using lsh;
using lsh.LshMath;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
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
    public void NearestNeighborSearch()
    {
        // build test distribution
        var random = new Random(0);

        var d = 100;
        var N = 2000;
        var data = Enumerable.Range(0, N).SelectMany(_ =>
        {
            var v = RandomVectors.RandomUniform(d, 0, 20, random);
            return (Vector<double>[])[v, v.Add(RandomVectors.RandomUniform(d, 0, 1, random))];
        }).ToArray();

        var watch = new Stopwatch();
        watch.Start();
        var dNn = ProbabilityDensityFunction.FromNnDistances(d, data, random);
        Console.WriteLine($"build nn dist: {watch.ElapsedMilliseconds} ms");

        watch.Restart();
        var dAny = ProbabilityDensityFunction.FromAnyDistances(d, data, random);
        Console.WriteLine($"build any dist: {watch.ElapsedMilliseconds} ms");

        dAny.Plot("dAny.svg");
        dNn.Plot("dNn.svg");

        var set = new ListLshSet<int>(LshParameters.Calculate(data.Length, d, 0.1, dNn, dAny));
        Console.WriteLine(set.Index.Params);

        // testing the algorithm
        watch.Restart();
        data.ForEach((v, i) => set.Add(v, i));
        Console.WriteLine($"Index Data: {watch.ElapsedMilliseconds} ms");
        new HistogramPDF(set.BucketSizes.Select(x => (double)x)).Plot("bucketSizes.svg");

        watch.Restart();
        List<double> foundNNDistances = new();
        data.ForEach((v, indexV) =>
        {
            var nn = set.Query(v, (p, indexP) => indexV != indexP);
            if (nn != null)
            {
                var dist = (nn.Value.P - v).L2Norm();
                foundNNDistances.Add(dist);
            }
        });
        Console.WriteLine($"Query Data: {watch.ElapsedMilliseconds} ms");
        new HistogramPDF(foundNNDistances.Where(x => x < 50)).Plot("foundNN.svg");
        double threshold = 7;
        Console.WriteLine($"Distance fraction above {threshold}: {foundNNDistances.Where(x => x > threshold).Count() / (double)foundNNDistances.Count}");
    }

    [Test]
    public void NearestNeighborMissRate_ExpectedNNDistance_QueryDistance()
    {
        // build test distribution
        var random = new Random(0);

        var d = 100;
        var N = 2000;
        var data = Enumerable.Range(0, N).Select(_ => RandomVectors.RandomUniform(d, 0, 20, random)).ToArray();

        var dAny = ProbabilityDensityFunction.FromAnyDistances(d, data, random);
        Plot plot = new();
        plot.PlottableList.Add(new LegendTitle("Exp Dist", plot));

        Plot plotPointsSearched = new();
        plotPointsSearched.PlottableList.Add(new LegendTitle("Exp Dist", plotPointsSearched));

        List<(double dist, int multiplications)> multiplications = [];
        foreach (var pulseValue in (double[])[5, 15, 20, 25, 30, /*50, 55*/])
        {
            var set = new ListLshSet<int>(LshParameters.Calculate(data.Length, d, 0.1, ProbabilityDensityFunction.FromUnitImpulse(pulseValue), dAny));
            multiplications.Add((pulseValue, set.Index.Params.NumberOfMultiplicationsPerQuery));

            data.ForEach(set.Add);

            List<(double dist, double searched)> pointsSearched = [];
            List<(double dist, double hitRate)> hitRates = [];
            for (double distance = 1; distance < 100; distance = distance * 1.2 + 1)
            {

                long found = 0;
                int sampleSize = 200;
                long searched = 0;
                data.Take(sampleSize).ForEach((v, indexV) =>
                {
                    var nn = set.Query(v + RandomVectors.RandomGivenLength(d, distance, random), (p, d) => { searched++; return true; });
                    if (nn != null && nn.Value.Data == indexV)
                    {
                        found++;
                    }
                });
                hitRates.Add((distance, (double)(sampleSize - found) / sampleSize));
                pointsSearched.Add((distance, (double)searched / sampleSize));
            }

            plot.Add.SignalXY(hitRates.Select(v => v.dist).ToArray(), hitRates.Select(v => v.hitRate).ToArray()).LegendText = "" + pulseValue;
            plotPointsSearched.Add.SignalXY(pointsSearched.Select(v => v.dist).ToArray(), pointsSearched.Select(v => v.searched).ToArray()).LegendText = "" + pulseValue;
        }
        plot.ShowLegend();
        plot.XLabel("Query Distance");
        plot.YLabel("Miss Rate");
        plot.SaveSvg("NearestNeighborMissRate_ExpectedNNDistance_QueryDistance.svg", 400, 300);

        plotPointsSearched.ShowLegend();
        plotPointsSearched.XLabel("Query Distance");
        plotPointsSearched.YLabel("Points Searched");
        plotPointsSearched.SaveSvg("NearestNeighborPointsSearched_ExpectedNNDistance_QueryDistance.svg", 400, 300);

        Plot plotMultiplications = new();
        plotMultiplications.Add.SignalXY(multiplications.Select(v => v.dist).ToArray(), multiplications.Select(v => v.multiplications).ToArray());
        plotMultiplications.XLabel("Expected NN Distance");
        plotMultiplications.YLabel("Multiplications");
        plotMultiplications.SaveSvg("NearestNeighborMultiplications_ExpectedNNDistance.svg", 400, 300);

    }

    [Test]
    public void NearestNeighborMissRate_Delta_QueryDistance()
    {
        // build test distribution
        var random = new Random(0);

        var d = 100;
        var N = 2000;
        var data = Enumerable.Range(0, N).Select(_ => RandomVectors.RandomUniform(d, 0, 20, random)).ToArray();

        var dAny = ProbabilityDensityFunction.FromAnyDistances(d, data, random);
        Plot plotMissRates = new();
        plotMissRates.PlottableList.Add(new LegendTitle("Delta", plotMissRates));
        Plot plotPointsSearched = new();
        plotPointsSearched.PlottableList.Add(new LegendTitle("Delta", plotPointsSearched));

        List<(double dist, int multiplications)> multiplications = [];
        foreach (var delta in (double[])[0.01, 0.05, 0.1, 0.2, 0.3, 0.5, 0.7, 0.8])
        {
            var set = new ListLshSet<int>(LshParameters.Calculate(data.Length, d, delta, ProbabilityDensityFunction.FromUnitImpulse(20), dAny));
            multiplications.Add((delta, set.Index.Params.NumberOfMultiplicationsPerQuery));

            data.ForEach(set.Add);

            List<(double dist, double hitRate)> hitRates = [];
            List<(double dist, double searched)> pointsSearched = [];
            for (double distance = 1; distance < 100; distance = distance * 1.2 + 1)
            {
                long searched = 0;
                long found = 0;
                int sampleSize = 200;
                data.Take(sampleSize).ForEach((v, indexV) =>
                {
                    var nn = set.Query(v + RandomVectors.RandomGivenLength(d, distance, random), (p, d) => { searched++; return true; });
                    if (nn != null && nn.Value.Data == indexV)
                    {
                        found++;
                    }
                });
                hitRates.Add((distance, (double)(sampleSize - found) / sampleSize));
                pointsSearched.Add((distance, (double)searched / sampleSize));
            }

            plotMissRates.Add.SignalXY(hitRates.Select(v => v.dist).ToArray(), hitRates.Select(v => v.hitRate).ToArray()).LegendText = "" + delta;
            plotPointsSearched.Add.SignalXY(pointsSearched.Select(v => v.dist).ToArray(), pointsSearched.Select(v => v.searched).ToArray()).LegendText = "" + delta;
        }
        plotMissRates.ShowLegend();
        plotMissRates.XLabel("Query Distance");
        plotMissRates.YLabel("Miss Rate");
        plotMissRates.SaveSvg("NearestNeighborMissRate_Delta_QueryDistance.svg", 400, 300);

        plotPointsSearched.ShowLegend();
        plotPointsSearched.XLabel("Query Distance");
        plotPointsSearched.YLabel("Points Searched");
        plotPointsSearched.SaveSvg("NearestNeighborPointsSearched_Delta_QueryDistance.svg", 400, 300);

        Plot plotMultiplications = new();
        plotMultiplications.Add.SignalXY(multiplications.Select(v => v.dist).ToArray(), multiplications.Select(v => v.multiplications).ToArray());
        plotMultiplications.XLabel("Delta");
        plotMultiplications.YLabel("Multiplications");
        plotMultiplications.SaveSvg("NearestNeighborPointsMultiplications_Delta.svg", 400, 300);

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

public class LegendTitle : IPlottable
{
    private readonly string text;
    private readonly LegendItem item;

    public LegendTitle(string text, Plot plot)
    {
        this.text = text;
        item = new LegendItem { LabelText = text, LabelBold = true };
        item.LabelStyle.OffsetX = -plot.Legend.SymbolWidth - plot.Legend.SymbolPadding;
    }

    public bool IsVisible { get; set; } = true;
    public IAxes Axes { get; set; } = new Axes();

    public IEnumerable<LegendItem> LegendItems => [item];

    public AxisLimits GetAxisLimits() => AxisLimits.NoLimits;

    public void Render(RenderPack rp)
    {
        // nop
    }
}