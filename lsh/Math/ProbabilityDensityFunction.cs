using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using ScottPlot;
using ScottPlot.DataSources;

namespace lsh.LshMath;

public static class ProbabilityDensityFunction
{
    public static bool UseAvx()
    {
        // return false;
        return Avx2.IsSupported;
    }
    public unsafe static HistogramPDF FromNnDistances(int dataDimensions, Vector<double>[] data, Random random, int sampleSize = 20)
    {
        var samples = data.SampleWithoutReplacementWithIndex(sampleSize, random).ToArray();
        return CalculateHistogram(dataDimensions, data, samples.Select(x => x.Data).ToArray(), (dataIndex, sampleIndex) => dataIndex != samples[sampleIndex].Index);
    }

    public static HistogramPDF FromAnyDistances(int dataDimensions, Vector<double>[] data, Random random, int sampleSize = 200)
    {
        var samples = data.SampleWithoutReplacement(sampleSize, random).ToArray();
        return CalculateHistogram(dataDimensions, samples, samples, (dataIndex, sampleIndex) => dataIndex != sampleIndex);
    }

    public static UnitImpulsePdf FromUnitImpulse(double value) => new UnitImpulsePdf(value);

    private static unsafe HistogramPDF CalculateHistogram(int dataDimensions, Vector<double>[] data, Vector<double>[] samples, Func<int, int, bool> filter)
    {
        var minDistances = new List<double>();
        if (UseAvx())
        {
            // fill data
            using var pData = new NativeMemoryWrapper<double>(data.Length * dataDimensions, 32);
            for (int i = 0; i < data.Length; i++)
                for (int j = 0; j < dataDimensions; j++)
                    pData[(i * dataDimensions) + j] = data[i][j];

            for (int sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
            {
                // sample
                var sample = samples[sampleIndex];
                using var pSample = new NativeMemoryWrapper<double>(dataDimensions, 32);
                var sampleArray = sample.ToArray();
                for (int i = 0; i < sampleArray.Length; i++)
                    pSample[i] = sampleArray[i];

                double minDist = double.PositiveInfinity;

                for (int j = 0; j < data.Length; j++)
                {
                    if (!filter(j, sampleIndex)) continue;
                    var pCurrentData = pData + j * dataDimensions;
                    Vector256<double> acc = Vector256<double>.Zero;
                    for (int i = 0; i < dataDimensions; i += 4)
                    {
                        var diff = Avx2.Subtract(Avx2.LoadAlignedVector256(pCurrentData + i), Avx2.LoadAlignedVector256(pSample + i));
                        acc = Avx2.Add(acc, Avx2.Multiply(diff, diff));
                    }

                    // build sum of accumulator values
                    double sum = 0;
                    for (int i = 0; i < 4; i++)
                        sum += acc[i];
                    var dist = Math.Sqrt(sum);
                    if (dist < minDist)
                    {
                        minDist = dist;
                    }
                }
                minDistances.Add(minDist);
            }
        }
        else
        {
            for (int sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
            {
                var sample = samples[sampleIndex];
                double minDist = double.PositiveInfinity;
                for (int j = 0; j < data.Length; j++)
                {
                    if (!filter(j, sampleIndex)) continue;
                    var dist = (sample - data[j]).L2Norm();
                    if (dist < minDist)
                    {
                        minDist = dist;
                    }
                }
                minDistances.Add(minDist);
            }
        }

        return new HistogramPDF(minDistances);
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

    public void Plot(Plot plot)
    {
        plot.Add.Function(x => Probability(x));
        plot.Axes.SetLimitsX(Mean - 4 * StandardDeviation, Mean + 4 * StandardDeviation);
        plot.Axes.SetLimitsY(0, 1.2 / Math.Sqrt(2 * Math.PI * StandardDeviation * StandardDeviation));
    }

    public override string ToString()
    => $"N({Mean},{StandardDeviation} ^2)";
}

public class UnitImpulsePdf : InputDataProbabilityDistribution
{
    public required double Value { get; init; }

    public UnitImpulsePdf() { }

    [SetsRequiredMembers]
    public UnitImpulsePdf(double value) { Value = value; }

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
        if (fileName.EndsWith(".svg"))
            plot.SaveSvg(fileName, 400, 300);
        else if (fileName.EndsWith(".png"))
            plot.SavePng(fileName, 400, 300);
        else throw new Exception("Unknown file format");
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
    => new GaussianPDF() { StandardDeviation = Math.Sqrt(BinWidth * Probabilities.Select(x => x.Probability * x.Center * x.Center).Sum()) };

}