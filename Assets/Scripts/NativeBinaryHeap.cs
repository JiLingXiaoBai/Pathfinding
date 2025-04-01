using System;
using Unity.Collections;

public struct NativeBinaryHeap<T> : IDisposable where T : unmanaged, IComparable<T>, IEquatable<T>
{
    public enum HeapType
    {
        Minimum,
        Maximum,
    }

    public HeapType Type { get; }

    private NativeArray<T> m_Items;
    private NativeHashMap<T, int> m_ItemIndices;
    public int Count { get; private set; }
    public int Capacity { get; }

    public int IndexOf(T item) => GetHeapIndex(item);

    public T this[int i] => m_Items[i];


    public NativeBinaryHeap(HeapType type, int maxHeapSize, Allocator allocator)
    {
        Type = type;
        m_Items = new NativeArray<T>(maxHeapSize, allocator, NativeArrayOptions.UninitializedMemory);
        m_ItemIndices = new NativeHashMap<T, int>(maxHeapSize, allocator);
        Count = 0;
        Capacity = maxHeapSize;
    }

    public void Add(T item)
    {
        if (Count >= Capacity)
            throw new InvalidOperationException("Heap is full");
        if (m_ItemIndices.ContainsKey(item))
            throw new InvalidOperationException("Item already exists in heap");
        UpdateHeapItem(item, Count);
        SortUp(item);
        Count++;
    }

    public T RemoveFirst()
    {
        T firstItem = m_Items[0];
        m_ItemIndices.Remove(firstItem);
        Count--;
        if (Count > 0)
        {
            T item = m_Items[Count];
            UpdateHeapItem(item, 0);
            SortDown(item);
        }
        return firstItem;
    }

    public T RemoveAt(int index)
    {
        if (Count == 0)
            throw new InvalidOperationException("Heap is empty");
        if (index < 0 || index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index out of range");
        T removedItem = m_Items[index];
        m_ItemIndices.Remove(removedItem);
        Count--;
        if (index == Count)
            return removedItem;
        T item = m_Items[Count];
        UpdateHeapItem(item, index);
        SortUp(item);
        SortDown(item);
        return removedItem;
    }


    private void SortDown(T item)
    {
        while (true)
        {
            int itemIndex = GetHeapIndex(item);
            int childIndexLeft = itemIndex * 2 + 1;
            if (childIndexLeft < Count)
            {
                int swapIndex = childIndexLeft;
                int childIndexRight = itemIndex * 2 + 2;

                if (Type == HeapType.Minimum)
                {
                    if (childIndexRight < Count &&
                        m_Items[childIndexLeft].CompareTo(m_Items[childIndexRight]) > 0)
                    {
                        swapIndex = childIndexRight;
                    }

                    if (item.CompareTo(m_Items[swapIndex]) > 0)
                    {
                        Swap(item, m_Items[swapIndex]);
                        continue;
                    }
                }
                else if (Type == HeapType.Maximum)
                {
                    if (childIndexRight < Count &&
                        m_Items[childIndexLeft].CompareTo(m_Items[childIndexRight]) < 0)
                    {
                        swapIndex = childIndexRight;
                    }
                    if (item.CompareTo(m_Items[swapIndex]) < 0)
                    {
                        Swap(item, m_Items[swapIndex]);
                        continue;
                    }
                }
            }
            break;
        }
    }

    private void SortUp(T item)
    {
        int index = GetHeapIndex(item);
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (Type == HeapType.Minimum && item.CompareTo(m_Items[parentIndex]) >= 0)
                break;
            if (Type == HeapType.Maximum && item.CompareTo(m_Items[parentIndex]) <= 0)
                break;
            Swap(item, m_Items[parentIndex]);
            index = parentIndex;
        }
    }

    private void Swap(T itemA, T itemB)
    {
        int indexA = GetHeapIndex(itemA);
        int indexB = GetHeapIndex(itemB);
        m_Items[indexA] = itemB;
        m_Items[indexB] = itemA;
        m_ItemIndices[itemA] = indexB;
        m_ItemIndices[itemB] = indexA;
    }

    private void UpdateHeapItem(T item, int newIndex)
    {
        m_ItemIndices.Remove(item);
        m_ItemIndices.TryAdd(item, newIndex);
        m_Items[newIndex] = item;
    }

    private int GetHeapIndex(T item) => m_ItemIndices.TryGetValue(item, out int index) ? index : -1;

    public void Dispose()
    {
        m_Items.Dispose();
        m_ItemIndices.Dispose();
    }
}