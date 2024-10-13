using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using lsh.LshMath;

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
        var K = (long)Math.Ceiling(-Math.Log(N) / Math.Log(pAny));
        var L = (long)Math.Ceiling(-Math.Log(delta) / Math.Pow(pNn, K));

        Params = new Parameters { W = W, K = K, L = L, DataDimensions = dataDimensions };
        this.Initialize();
    }

    private class Table
    {
        public readonly List<(DenseVector V, double B)> Vectors = [];
        public Dictionary<long, List<(DenseVector P, T Data)>> Buckets = [];
    }

    private readonly List<Table> Tables = [];

    private void Initialize()
    {
        var random = new Random(0);
        for (int i = 0; i < Params.L; i++)
        {
            var table = new Table();
            for (int j = 0; j < Params.K; j++)
            {
                table.Vectors.Add((DenseVector.RandomNormal(Params.DataDimensions, random), random.NextDouble() * Params.W));
            }
            Tables.Add(table);
        }
    }

    public void Add(DenseVector p, T data)
    {
        Tables.ForEach(t =>
        {
            long bucket = CalculateBucket(p, t);
            t.Buckets.ComputeIfAbsent(bucket, () => []).Add((p, data));
        });
    }

    private long CalculateBucket(DenseVector p, Table t) => t.Vectors.Select(v => (long)Math.Floor((v.V.Dot(p) + v.B) / Params.W)).GetSequenceHashCode();

    public (DenseVector P, T Data)? Query(DenseVector q)
    {
        var min = Tables.Select(t =>
        {
            long bucket = CalculateBucket(q, t);
            return t.Buckets.GetValueOrDefault(bucket)?.MinBy(entry => entry.P.DistanceTo(q));
        }).Where(x => x != null).MinBy(entry => entry!.Value.P.DistanceTo(q));
        return min;
    }


    public override string ToString()
    => $"LshSet(W: {Params.W}, K: {Params.K}, L: {Params.L})";

    public record Parameters
    {
        public required double W { get; init; }
        public required long K { get; init; }
        public required long L { get; init; }

        public required int DataDimensions { get; init; }
    }
}
