namespace Aiursoft.Canon.Models;

public class SafeQueue<T>
{
    private readonly object _loc = new();
    private readonly Queue<T> _queue = new();

    public void Enqueue(T item)
    {
        lock (_loc)
        {
            _queue.Enqueue(item);
        }
    }

    public T Dequeue()
    {
        T item;
        lock (_loc)
        {
            item = _queue.Dequeue();
        }

        return item;
    }

    public bool Any()
    {
        bool any;
        lock (_loc)
        {
            any = _queue.Any();
        }

        return any;
    }

    public int Count()
    {
        int count;
        lock (_loc)
        {
            count = _queue.Count;
        }

        return count;
    }
}
