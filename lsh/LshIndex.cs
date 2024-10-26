using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using lsh.LshMath;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;

namespace lsh;

public class LshIndex<TBucket> : IDisposable
{
    public LshParameters Params { get; private init; }
    public readonly BucketStorage<TBucket> Storage;

    private readonly List<Table> Tables = [];

    [SetsRequiredMembers]
    public LshIndex(LshParameters parameters, BucketStorage<TBucket> bucketStorage)
    {
        Params = parameters;
        this.Storage = bucketStorage;
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
                VectorsAvx = Vectors,
                BsAvx = bs
            });
        }
    }



    private class Table
    {
        // public readonly List<(Vector<double> V, double B)> Vectors = [];
        public required Matrix<double> Vectors;
        public required NativeMemoryWrapper<double> VectorsAvx;
        public required Vector<double> Bs;
        public required double[] BsAvx;
    }


    /// <summary>
    /// Add a point to this set
    /// </summary>
    /// <param name="p">Point to add to the set</param>
    /// <param name="bucketFactory"> Factory to create new empty buckets </param>
    /// <param name="addToBucket" >Function to add the point to a bucket</param>
    public void Add(Vector<double> p, Action<TBucket> addToBucket)
    {
        for (int i = 0; i < Tables.Count; i++)
        {
            addToBucket(Storage.GetOrCreate(i, CalculateBucket(p, Tables[i])));
        }
    }

    /// <summary>
    /// Find all buckets matching the given query vector
    /// </summery>
    public List<TBucket> Query(Vector<double> q)
    {
        List<TBucket> buckets = new();
        for (int i = 0; i < Tables.Count; i++)
        {
            var bucket = Storage.Get(i, CalculateBucket(q, Tables[i]));
            if (bucket != null)
            {
                buckets.Add(bucket);
            }
        }
        return buckets;
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
        double* pTable = table.VectorsAvx;
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


            results.Add(Math.Floor((sum + table.BsAvx[i]) / Params.W));
        }

        return results.GetSequenceHashCode();
    }

    public override string ToString()
    => $"LshSet(W: {Params.W}, K: {Params.K}, L: {Params.L})";

    private bool disposed = false;
    public void Dispose()
    {
        if (!disposed)
        {
            Tables.ForEach(t => t.VectorsAvx.Dispose());
            disposed = true;
        }
    }
}

public record LshParameters
{
    public required double W { get; init; }
    public required int K { get; init; }
    public required int L { get; init; }

    public required int DataDimensions { get; init; }

    public static LshParameters Calculate(long N, int dataDimensions, double delta, InputDataProbabilityDistribution dNn, InputDataProbabilityDistribution dAny)
    {
        var gNn = dNn.MultiplyWithUnitNormal();
        var gAny = dAny.MultiplyWithUnitNormal();

        var W = FindArgMax.Compute(x => Math.Log(gAny.TriangleProbability(x)) / Math.Log(gNn.TriangleProbability(x)), 1e-3);

        var pNn = gNn.TriangleProbability(W);
        var pAny = gAny.TriangleProbability(W);
        var K = (int)Math.Ceiling(-Math.Log(N) / Math.Log(pAny));
        var L = (int)Math.Ceiling(-Math.Log(delta) / Math.Pow(pNn, K));
        return new LshParameters { W = W, K = K, L = L, DataDimensions = dataDimensions };
    }

    public int NumberOfMultiplicationsPerQuery
    => DataDimensions * K * L;


    public override string ToString()
    => $"(W: {W}, K: {K}, L: {L})";
}

