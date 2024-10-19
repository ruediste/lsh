using System.Diagnostics.CodeAnalysis;
using lsh.LshMath;
using MathNet.Numerics;

namespace lsh;

public class LshSet<T>
{
    public Parameters Params { get; private init; }

    [SetsRequiredMembers]
    public LshSet(Parameters parameters)
    {
        Params = parameters;
        this.Initialize();
    }

    [SetsRequiredMembers]
    public LshSet(long N, int dataDimensions, double delta, InputDataProbabilityDistribution dNn, InputDataProbabilityDistribution dAny)
    {
        var gNn = dNn.MultiplyWithUnitNormal();
        var gAny = dAny.MultiplyWithUnitNormal();

        var W = FindArgMax.Compute(x => Math.Log(gAny.TriangleProbability(x)) / Math.Log(gNn.TriangleProbability(x)), 1e-3);

        var pNn = gNn.TriangleProbability(W);
        var pAny = gAny.TriangleProbability(W);
        var K = (int)Math.Ceiling(-Math.Log(N) / Math.Log(pAny));
        var L = (int)Math.Ceiling(-Math.Log(delta) / Math.Pow(pNn, K));

        Params = new Parameters { W = W, K = K, L = L, DataDimensions = dataDimensions };
        this.Initialize();
    }

    private class Table
    {
        // public readonly List<(Vector<double> V, double B)> Vectors = [];
        public required Matrix<double> Vectors;
        public required Vector<double> Bs;
        public Dictionary<long, List<(Vector<double> P, T Data)>> Buckets = [];
    }

    private readonly List<Table> Tables = [];

    private void Initialize()
    {
        var random = new Random(0);
        for (int i = 0; i < Params.L; i++)
        {

            Tables.Add(new Table()
            {
                Vectors = Matrix<double>.Build.Dense(Params.K, Params.DataDimensions, (i, j) => random.NextGaussian()),
                Bs = Vector<double>.Build.DenseOfEnumerable(Generate.UniformSequence().Take(Params.K))
            });
        }
    }



    public void Add(Vector<double> p, T data)
    {
        Tables.ForEach(t =>
        {
            long bucket = CalculateBucket(p, t);
            t.Buckets.ComputeIfAbsent(bucket, () => []).Add((p, data));
        });
    }

    private long CalculateBucket(Vector<double> p, Table t) => ((t.Vectors * p + t.Bs) / Params.W).PointwiseFloor().GetSequenceHashCode();

    public (Vector<double> P, T Data)? Query(Vector<double> q)
    {
        var min = Tables.Select(t =>
        {
            long bucket = CalculateBucket(q, t);
            return t.Buckets.GetValueOrDefault(bucket)?.MinBy(entry => (entry.P - q).L2Norm());
        }).Where(x => x != null).MinBy(entry => (entry!.Value.P - q).L2Norm());
        return min;
    }


    public override string ToString()
    => $"LshSet(W: {Params.W}, K: {Params.K}, L: {Params.L})";

    public record Parameters
    {
        public required double W { get; init; }
        public required int K { get; init; }
        public required int L { get; init; }

        public required int DataDimensions { get; init; }
    }
}
