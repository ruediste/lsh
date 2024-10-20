using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using lsh.LshMath;
using MathNet.Numerics;

namespace lsh;

public class LshSet<T> : IDisposable
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

        Console.WriteLine($"gnn:{gNn} gany:{gAny}");
        var W = FindArgMax.Compute(x => Math.Log(gAny.TriangleProbability(x)) / Math.Log(gNn.TriangleProbability(x)), 1e-3);
        var pNn = gNn.TriangleProbability(W);
        var pAny = gAny.TriangleProbability(W);
        Console.WriteLine($"pnn: {pNn} pAny: {pAny}");
        var K = (int)Math.Ceiling(-Math.Log(N) / Math.Log(pAny));
        var L = (int)Math.Ceiling(-Math.Log(delta) / Math.Pow(pNn, K));

        Params = new Parameters { W = W, K = K, L = L, DataDimensions = dataDimensions };
        this.Initialize();
    }

    private class Table
    {
        // public readonly List<(Vector<double> V, double B)> Vectors = [];
        public required Matrix<double> Vectors;
        public required NativeMemoryWrapper<double> Vectors2;
        public required Vector<double> Bs;
        public required double[] Bs2;
        public Dictionary<long, List<(Vector<double> P, T Data)>> Buckets = [];
    }

    private readonly List<Table> Tables = [];

    private void Initialize()
    {
        var random = new Random(0);
        for (int i = 0; i < Params.L; i++)
        {
            NativeMemoryWrapper<double> Vectors = new(Params.K * Params.DataDimensions, 32);
            for (int j = 0; j < Params.K * Params.DataDimensions; j++)
            {
                Vectors[j] = random.NextGaussian();
            }

            double[] bs = new double[Params.K];
            for (int j = 0; j < Params.K; j++)
                bs[j] = random.NextDouble() * Params.W;

            Tables.Add(new Table()
            {
                Vectors = Matrix<double>.Build.Dense(Params.K, Params.DataDimensions, (i, j) => Vectors[(i * Params.DataDimensions) + j]),
                Bs = Vector<double>.Build.Dense(Params.K, i => bs[i]),
                Vectors2 = Vectors,
                Bs2 = bs
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
    private long CalculateBucket(Vector<double> p, Table t) => !Avx2.IsSupported || Params.DataDimensions < 500 ? CalculateBucketMathnet(p, t) : CalculateBucketAvx(p, t);
    private long CalculateBucketMathnet(Vector<double> p, Table t) => ((t.Vectors * p + t.Bs) / Params.W).PointwiseFloor().GetSequenceHashCode();
    private unsafe long CalculateBucketAvx(Vector<double> p, Table table)
    {
        if (p.Count != Params.DataDimensions)
            throw new Exception("Invalid vector size");

        using var pMem = new NativeMemoryWrapper<double>(Params.DataDimensions, 32);
        p.ToArray().CopyTo(new Span<double>(pMem.Data, Params.DataDimensions));

        var results = new List<double>();
        double* pTable = table.Vectors2;
        for (int i = 0; i < Params.K; i++)
        {
            Vector256<double> acc = Vector256<double>.Zero;
            double* pData = pMem;
            for (int j = 0; j < Params.DataDimensions; j += 4)
            {
                acc = Avx2.Add(acc, Avx2.Multiply(Avx2.LoadAlignedVector256(pTable), Avx2.LoadAlignedVector256(pData)));
                pTable += 4;
                pData += 4;
            }

            double sum = 0;
            for (int j = 0; j < 4; j++)
                sum += acc[j];


            results.Add(Math.Floor((sum + table.Bs2[i]) / Params.W));
        }

        return results.GetSequenceHashCode();
    }

    public IEnumerable<int> BucketSizes => this.Tables.SelectMany(t => t.Buckets.Select(b => b.Value.Count));

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

    private bool disposed = false;
    public void Dispose()
    {
        if (!disposed)
        {
            Tables.ForEach(t => t.Vectors2.Dispose());
            disposed = true;
        }
    }

    public record Parameters
    {
        public required double W { get; init; }
        public required int K { get; init; }
        public required int L { get; init; }

        public required int DataDimensions { get; init; }
    }
}
