using System;
using Unity.Collections;

public enum NativeBinaryHeapType
{
    Minimum,
    Maximum,
}

public struct NativeBinaryHeap<T> : IDisposable where T : unmanaged, IComparable<T>, IEquatable<T>
{
    public NativeBinaryHeapType Type { get; }

    private NativeList<T> m_Items;
    private NativeHashMap<T, int> m_ItemIndices;
    public int Count { get; private set; }

    public int IndexOf(T item) => GetHeapIndex(item);

    public T this[int i] => m_Items[i];


    public NativeBinaryHeap(NativeBinaryHeapType type, int initialCapacity, Allocator allocator)
    {
        Type = type;
        m_Items = new NativeList<T>(allocator);
        m_Items.Resize(initialCapacity, NativeArrayOptions.UninitializedMemory);
        m_ItemIndices = new NativeHashMap<T, int>(initialCapacity, allocator);
        Count = 0;
    }

    public void Add(T item)
    {
        if (m_ItemIndices.ContainsKey(item))
            throw new InvalidOperationException("Item already exists in heap");

        if (Count >= m_Items.Length)
        {
            int newCapacity = m_Items.Length == 0 ? 4 : m_Items.Length * 2;
            m_Items.Resize(newCapacity, NativeArrayOptions.UninitializedMemory);
        }
        
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

                if (Type == NativeBinaryHeapType.Minimum)
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
                else if (Type == NativeBinaryHeapType.Maximum)
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
            if (Type == NativeBinaryHeapType.Minimum && item.CompareTo(m_Items[parentIndex]) >= 0)
                break;
            if (Type == NativeBinaryHeapType.Maximum && item.CompareTo(m_Items[parentIndex]) <= 0)
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