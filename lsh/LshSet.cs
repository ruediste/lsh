namespace lsh;

public interface LshSet<T>
{
    public void Add(Vector<double> p, T data);
    public (Vector<double> P, T Data)? Query(Vector<double> q, Func<Vector<double>, T, bool>? filter = null);
}

public class ListLshSet<T> : LshSet<T>
{
    public readonly LshIndex<List<(Vector<double> P, T Data)>> Index;

    public ListLshSet(LshParameters lshParams)
    : this(lshParams, new DictionaryBucketStorage<List<(Vector<double> P, T Data)>>(() => []))
    {
    }

    public ListLshSet(LshParameters lshParams, BucketStorage<List<(Vector<double> P, T Data)>> storage)
    {
        Index = new LshIndex<List<(Vector<double> P, T Data)>>(lshParams, storage);
    }

    public void Add(Vector<double> p, T data) => Index.Add(p, b => b.Add((p, data)));

    public (Vector<double> P, T Data)? Query(Vector<double> q, Func<Vector<double>, T, bool>? filter = null)
    {
        (Vector<double> P, T Data)? result = null;
        double? minDistance = null;
        foreach (var bucket in Index.Query(q))
        {
            foreach (var entry in bucket.Where(entry => filter == null || filter(entry.P, entry.Data)))
            {
                var distance = (entry.P - q).L2Norm();
                if (result == null || minDistance == null || distance < minDistance)
                {
                    result = entry;
                    minDistance = distance;
                }
            }

        }
        return result;
    }

    public IEnumerable<int> BucketSizes => Index.Storage.Buckets.Select(b => b.Count);
}