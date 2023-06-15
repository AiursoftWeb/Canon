using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.Canon.Tests;

[TestClass]
public class CanonPoolTests
{
    private IServiceProvider? _serviceProvider;

    [TestInitialize]
    public void Init()
    {
        _serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddTaskCanon()
            .BuildServiceProvider();
    }

    [TestMethod]
    public void TestAddTaskToPool()
    {
        // Arrange
        var pool = _serviceProvider?.GetRequiredService<CanonPool>();
        var taskAdded = false;

        // Act
        pool?.RegisterNewTaskToPool(() =>
        {
            taskAdded = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.IsFalse(taskAdded);
    }

    [TestMethod]
    public async Task TestRunAllTasksInPool()
    {
        // Arrange
        var pool = _serviceProvider?.GetRequiredService<CanonPool>();
        var maxDegreeOfParallelism = 2;
        var tasksExecuted = new List<int>();

        // Add tasks to the pool
        for (int i = 0; i < 10; i++)
        {
            var taskNumber = i;
            pool?.RegisterNewTaskToPool(() =>
            {
                tasksExecuted.Add(taskNumber);
                return Task.CompletedTask;
            });
        }

        // Act
        await pool?.RunAllTasksInPoolAsync(maxDegreeOfParallelism)!;

        // Assert
        Assert.AreEqual(10, tasksExecuted.Count);
        for (int i = 0; i < 10; i++)
        {
            Assert.AreEqual(i, tasksExecuted[i]);
        }
    }

    [TestMethod]
    public async Task TestEmptyQueueAfterExecution()
    {
        // Arrange
        var pool = _serviceProvider?.GetRequiredService<CanonPool>();
        var taskAdded = false;

        // Add tasks to the pool
        pool?.RegisterNewTaskToPool(async () =>
        {
            await Task.Delay(1);
            taskAdded = true;
        });

        // Act
        await pool?.RunAllTasksInPoolAsync()!;

        // Assert
        Assert.IsTrue(taskAdded);
    }
}
