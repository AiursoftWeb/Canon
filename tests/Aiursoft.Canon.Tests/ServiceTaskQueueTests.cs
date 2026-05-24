using Aiursoft.Canon.TaskQueue;

namespace Aiursoft.Canon.Tests;

[TestClass]
public class ServiceTaskQueueTests
{
    [TestMethod]
    public void HasPendingOrProcessingTasks_QueueDoesNotExist_ReturnsFalse()
    {
        var queue = new ServiceTaskQueue();

        var result = queue.HasPendingOrProcessingTasks("nonexistent");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasPendingOrProcessingTasks_QueueHasPendingTasks_ReturnsTrue()
    {
        var queue = new ServiceTaskQueue();
        queue.QueueWithDependency<object>("myQueue", "test", _ => Task.CompletedTask);

        var result = queue.HasPendingOrProcessingTasks("myQueue");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasPendingOrProcessingTasks_QueueProcessing_ReturnsTrue()
    {
        var queue = new ServiceTaskQueue();
        queue.QueueWithDependency<object>("myQueue", "test", _ => Task.CompletedTask);
        var task = queue.TryDequeueNextTask("myQueue");

        Assert.IsNotNull(task);
        var result = queue.HasPendingOrProcessingTasks("myQueue");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HasPendingOrProcessingTasks_AfterCompletion_ReturnsFalse()
    {
        var queue = new ServiceTaskQueue();
        queue.QueueWithDependency<object>("myQueue", "test", _ => Task.CompletedTask);
        var task = queue.TryDequeueNextTask("myQueue");
        queue.CompleteTask(task!.TaskId, true);

        var result = queue.HasPendingOrProcessingTasks("myQueue");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasPendingOrProcessingTasks_MultipleQueues_Independent()
    {
        var queue = new ServiceTaskQueue();
        queue.QueueWithDependency<object>("queueA", "test", _ => Task.CompletedTask);

        Assert.IsTrue(queue.HasPendingOrProcessingTasks("queueA"));
        Assert.IsFalse(queue.HasPendingOrProcessingTasks("queueB"));
    }
}
