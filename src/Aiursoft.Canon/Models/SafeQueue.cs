namespace Aiursoft.Canon.Models;

public class SafeQueue<T>
{
    private readonly object loc = new();
    private readonly Queue<T> queue = new();

    public void Enqueue(T item)
    {
        lock (loc)
        {
            queue.Enqueue(item);
        }
    }

    public T Dequeue()
    {
        T item;
        lock (loc)
        {
            item = queue.Dequeue();
        }

        return item;
    }

    public bool Any()
    {
        return queue.Any();
    }

    public int Count()
    {
        return queue.Count;
    }
}
