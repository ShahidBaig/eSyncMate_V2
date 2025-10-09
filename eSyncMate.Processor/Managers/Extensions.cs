using System.Collections.Generic;
using System.Linq;

public static class Extensions
{
    // Extension method to split a list into smaller batches
    public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> items, int batchSize)
    {
        List<T> batch = new List<T>(batchSize);
        foreach (var item in items)
        {
            batch.Add(item);
            if (batch.Count >= batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }
        if (batch.Count > 0) yield return batch;
    }
}
