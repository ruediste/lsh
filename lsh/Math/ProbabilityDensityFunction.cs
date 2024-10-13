using ScottPlot;

namespace lsh.LshMath;

public static class ProbabilityDensityFunction
{
    public static (HistogramPDF dNn, HistogramPDF dAny) FromDistances(DenseVector[] data)
    {
        // calculate d_any
        var distances = new List<double>();
        var minDistances = new List<double>();
        for (int i = 0; i < data.Length; i++)
        {
            double minDist = double.PositiveInfinity;
            for (int j = 0; j < data.Length; j++)
            {
                if (i == j) continue;
                var dist = data[i].DistanceTo(data[j]);
                if (j > i)
                    distances.Add(dist);
                if (dist < minDist)
                {
                    minDist = dist;
                }
            }
            minDistances.Add(minDist);
        }

        return (dNn: new HistogramPDF(minDistances), dAny: new HistogramPDF(distances));
    }
}

public interface InputDataProbabilityDistribution
{
    GaussianPDF MultiplyWithUnitNormal();
}

public class GaussianPDF
{
    public double Mean { get; init; } = 0;
    public double StandardDeviation { get; init; } = 1;
    public double Probability(double x)
    {
        return 1 / (StandardDeviation * Math.Sqrt(2 * Math.PI)) * Math.Exp(-(Math.Pow(x - Mean, 2) / (2 * StandardDeviation * StandardDeviation)));
    }

    public double MaxIntegrationStepSize(double x)
    {
        var distFromMean = Math.Abs(Mean - x);
        if (distFromMean > 6 * StandardDeviation)
            return StandardDeviation / 4;
        return StandardDeviation / 20;
    }

    public double TriangleProbability(double w)
    {
        var result = 0.0;
        var x = -w;
        while (x < w)
        {
            var step = Math.Min(w / 10, MaxIntegrationStepSize(x));
            result += step * (1 - Math.Abs(x / w)) * Probability(x);
            x += step;
        }
        return result;
    }

    public override string ToString()
    => $"N({Mean},{StandardDeviation} ^2)";
}

public class UnitImpulsePdf : InputDataProbabilityDistribution
{
    public required double Value { get; init; }

    public GaussianPDF MultiplyWithUnitNormal()
    => new GaussianPDF() { StandardDeviation = Value };
}

public class HistogramPDF : InputDataProbabilityDistribution
{
    public readonly double Min;
    public readonly double Max;
    public readonly double BinWidth;
    public readonly double[] Bins;
    public HistogramPDF(IEnumerable<double> values)
    {
        Min = double.PositiveInfinity;
        Max = double.NegativeInfinity;
        var count = 0;
        foreach (var value in values)
        {
            count++;
            if (value < Min)
                Min = value;
            if (value > Max)
                Max = value;
        }

        var binCount = (int)Math.Ceiling(1 + Math.Log2(count));
        BinWidth = Max == Min ? 1 : (Max - Min) / binCount;
        Bins = new double[binCount];
        foreach (var value in values)
        {
            Bins[Math.Min(binCount - 1, (int)((value - Min) / BinWidth))]++;
        }
        var factor = 1.0 / (BinWidth * count);
        for (var i = 0; i < Bins.Length; i++)
            Bins[i] *= factor;
    }

    public void Plot(string fileName)
    {
        Plot plot = new();
        Plot(plot);
        plot.SavePng(fileName, 400, 300);
    }
    public void Plot(Plot plot)
    {
        plot.Add.Bars(Bins.Select((count, index) => new Bar
        {
            Position = Min + (BinWidth * (index + 0.5)),
            Value = count,
            Size = BinWidth
        }));
    }

    public double Probability(double x)
    {
        var bin = (int)((x - Min) / BinWidth);
        if (bin < Bins.Length && bin >= 0)
            return Bins[bin];
        return 0;
    }

    public IEnumerable<(double Center, double Probability)> Probabilities
    => Bins.Select((p, index) => (Center: Min + (BinWidth * (index + 0.5)), Probability: p));

    public override string ToString()
    {
        return $"Min: {Min} Max: {Max} BinWidth: {BinWidth} Count: {Bins.Length}";
    }

    public GaussianPDF MultiplyWithUnitNormal()
    => new GaussianPDF() { StandardDeviation = Math.Sqrt(Probabilities.Select(x => x.Probability * x.Center * x.Center).Sum()) };

}