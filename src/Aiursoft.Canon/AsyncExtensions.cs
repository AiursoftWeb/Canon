namespace Aiursoft.Canon;

public static class AsyncExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> enumerable)
    {
        var result = new List<T>();
        await foreach (var item in enumerable)
        {
            result.Add(item);
        }
        return result;
    }

    public static async IAsyncEnumerable<T> Take<T>(this IAsyncEnumerable<T> enumerable, int take)
    {
        var count = 0;
        await foreach (var item in enumerable)
        {
            if (count >= take)
            {
                yield break;
            }
            yield return item;
            count++;
        }
    }
}