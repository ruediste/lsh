using ScottPlot;

namespace lsh.LshMath;

public interface ProbabilityDensityFunction
{
    double Probability(double x);

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

public class GaussianPDF : ProbabilityDensityFunction
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

public class UnitImpulsePdf
{
    public required double Value { get; init; }
}

// <summary>
/// A probability density function that is the product of two other PDFs. The general formula to use is:
/// <code>
/// Z(y)=int(-infinity,+infinity, x=>X(x)*Y(y/x)*1/abs(x))
/// </code>
/// </summary>
/// <see cref="https://en.wikipedia.org/wiki/Distribution_of_the_product_of_two_random_variables"/>
public class MultiplicationPDF(Func<double, double> func) : ProbabilityDensityFunction
{
    public double Probability(double x) => func(x);

    public static GaussianPDF CreateUnitNormal(HistogramPDF left)
    => new GaussianPDF() { StandardDeviation = Math.Sqrt(left.Probabilities.Select(x => x.Probability * x.Center * x.Center).Sum()) };

    public static GaussianPDF CreateUnitNormal(UnitImpulsePdf left)
    => new GaussianPDF() { StandardDeviation = left.Value };
}

public class HistogramPDF : ProbabilityDensityFunction
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
        BinWidth = (Max - Min) / binCount;
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
}