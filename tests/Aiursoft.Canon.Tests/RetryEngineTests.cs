using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.Canon.Tests;

[TestClass]
public class RetryEngineTests
{
    private readonly ILogger<RetryEngine> _logger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger<RetryEngine>();

    [TestMethod]
    public async Task RunWithRetry_ThrowsExceptionAfterMaxAttempts()
    {
        var engine = new RetryEngine(_logger);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            engine.RunWithRetry<int>(_ => throw new InvalidOperationException("Test exception"), attempts: 3));
    }

    [TestMethod]
    public async Task RunWithRetry_ReturnsResultOnSuccess()
    {
        var engine = new RetryEngine(_logger);

        var result = await engine.RunWithRetry(attempt =>
        {
            if (attempt == 2)
            {
                return Task.FromResult(42);
            }

            throw new Exception("Test exception");
        }, attempts: 3);

        Assert.AreEqual(42, result);
    }
}
