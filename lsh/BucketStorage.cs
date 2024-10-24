using lsh.LshMath;

namespace lsh;

public interface BucketStorage<TBucket>
{
    TBucket GetOrCreate(int tableId, long bucketId);
    TBucket? Get(int tableId, long bucketId);

    IEnumerable<TBucket> Buckets { get; }
}

public class DictionaryBucketStorage<TBucket>(Func<TBucket> bucketFactory) : BucketStorage<TBucket>
{
    private readonly Dictionary<(int TableId, long BucketId), TBucket> buckets = [];
    public TBucket? Get(int tableId, long bucketId)
    => buckets.TryGetValue((tableId, bucketId), out var bucket) ? bucket : default;

    public TBucket GetOrCreate(int tableId, long bucketId)
    => buckets.ComputeIfAbsent((tableId, bucketId), bucketFactory);

    public IEnumerable<TBucket> Buckets => buckets.Values;
}
