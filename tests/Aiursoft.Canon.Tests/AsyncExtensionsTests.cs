[assembly:DoNotParallelize]

namespace Aiursoft.Canon.Tests;

[TestClass]
public class AsyncExtensionsTests
{
    [TestMethod]
    public async Task TestCanonAsync()
    {
        var fibonacci = FibonacciAsync();
        var top100 = fibonacci.Take(30);
        var result = top100.ToListAsync();
        Assert.HasCount(30, await result);
    }

    private async IAsyncEnumerable<int> FibonacciAsync()
    {
        int current = 1, next = 1;

        while (true)
        {
            await Task.Delay(1);
            yield return current;
            next = current + (current = next);
        }

        // ReSharper disable once IteratorNeverReturns
    }
}
