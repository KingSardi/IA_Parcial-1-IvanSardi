
using System.Collections.Generic;

public class MinPriorityQueue<T>
{
    private struct Entry
    {
        public T Item;
        public float Priority;

        public Entry(T item, float priority)
        {
            Item = item;
            Priority = priority;
        }
    }

    private readonly List<Entry> _heap = new();

    public int Count => _heap.Count;

    public void Enqueue(T item, float priority)
    {
        _heap.Add(new Entry(item, priority));

        int index = _heap.Count - 1;

        while (index > 0)
        {
            int parent = (index - 1) / 2;

            if (_heap[parent].Priority <= _heap[index].Priority)
                break;

            (_heap[parent], _heap[index]) =
                (_heap[index], _heap[parent]);

            index = parent;
        }
    }

    public T Dequeue()
    {
        T result = _heap[0].Item;

        int lastIndex = _heap.Count - 1;

        _heap[0] = _heap[lastIndex];
        _heap.RemoveAt(lastIndex);

        int index = 0;

        while (true)
        {
            int left = index * 2 + 1;
            int right = index * 2 + 2;
            int smallest = index;

            if (left < _heap.Count &&
                _heap[left].Priority < _heap[smallest].Priority)
            {
                smallest = left;
            }

            if (right < _heap.Count &&
                _heap[right].Priority < _heap[smallest].Priority)
            {
                smallest = right;
            }

            if (smallest == index)
                break;

            (_heap[index], _heap[smallest]) =
                (_heap[smallest], _heap[index]);

            index = smallest;
        }

        return result;
    }

    public void Clear()
    {
        _heap.Clear();
    }
}