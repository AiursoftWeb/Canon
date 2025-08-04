using System.Net;
using Microsoft.Extensions.Logging;

namespace Aiursoft.Canon.Tests;

[TestClass]
public class RetryEngineTests
{
    private readonly ILogger<RetryEngine> _logger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger<RetryEngine>();

    [TestMethod]
    public async Task RunWithRetry_ThrowsExceptionAfterMaxAttempts()
    {
        var engine = new RetryEngine(_logger);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            engine.RunWithRetry<int>(_ => throw new InvalidOperationException("Test exception"), attempts: 3));
    }

    [TestMethod]
    public async Task RunWithRetryWithoutResponse_ReturnsResultOnSuccess()
    {
        var engine = new RetryEngine(_logger);

        await engine.RunWithRetry(attempt =>
        {
            if (attempt == 2)
            {
                return Task.CompletedTask;
            }

            throw new Exception("Test exception");
        }, attempts: 3);
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

    [TestMethod]
    public async Task RunWithRetry_ShouldNotRetry()
    {
        var engine = new RetryEngine(_logger);

        try
        {
            await engine.RunWithRetry<int>(
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                attempt =>
                {
                    if (attempt == 1)
                    {
                        throw new BadImageFormatException("Test BadImageFormatException");
                    }
                    throw new WebException("Test exception");
                },
                attempts: int.MaxValue,
                when: e => e is BadImageFormatException);
            Assert.Fail("Should not success!");
        }
        catch (Exception e)
        {
            Assert.IsTrue(e is WebException);
        }
    }
}
