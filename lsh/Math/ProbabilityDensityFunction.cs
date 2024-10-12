using ScottPlot;

namespace lsh.LshMath;

public interface ProbabilityDensityFunction
{
    double Probability(double x);
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
        if (distFromMean > 3 * StandardDeviation)
            return StandardDeviation / 2;
        return StandardDeviation / 10;
    }

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

    public static MultiplicationPDF Create(HistogramPDF left, GaussianPDF right)
    => new MultiplicationPDF((y) =>
    {
        var result = 0.0;
        var x = left.Min;
        while (x < left.Max)
        {
            result += left.Probability(x) * right.Probability(y / x) * 1 / Math.Abs(x);
            x += Math.Min(left.BinWidth, right.MaxIntegrationStepSize(x));
        }
        return result;
    });

    public static MultiplicationPDF Create(UnitImpulsePdf left, GaussianPDF right)
    => new MultiplicationPDF((y) => right.Probability(y / left.Value) * 1 / Math.Abs(left.Value));

    public double TriangleProbability(double w)
    {
        var result = 0.0;
        for (int i = 0; i
    }
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
        var factor = 1.0 / count;
        for (var i = 0; i < Bins.Length; i++)
            Bins[i] *= factor;
    }

    public void Plot(string fileName)
    {
        Plot myPlot = new();
        myPlot.Add.Bars(Bins.Select((count, index) => new Bar
        {
            Position = Min + (BinWidth * index),
            Value = count,
            Size = BinWidth
        }));
        myPlot.SavePng(fileName, 400, 300);
    }

    public double Probability(double x)
    {
        var bin = (int)((x - Min) / BinWidth);
        if (bin < Bins.Length && bin >= 0)
            return Bins[bin];
        return 0;
    }

    public override string ToString()
    {
        return $"Min: {Min} Max: {Max} BinWidth: {BinWidth} Count: {Bins.Length}";
    }
}