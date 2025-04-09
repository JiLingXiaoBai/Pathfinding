using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


public struct KDTreeNode
{
    public int left;
    public int right;

    public int begin;
    public int end;

    public float maxX;
    public float maxY;
    public float minX;
    public float minY;
}

public interface IKDTreeItem
{
    public float2 Position2D { get; }
}

[BurstCompile]
public struct KDTreeJob<T> : IJob where T : struct, IKDTreeItem
{
    public NativeArray<T> agentDataArray;
    public NativeArray<KDTreeNode> kdTreeNodeArray;
    public int maxLeavesCount;
    public bool recompute;
    private int m_CurrentNodeIndex;

    public void Execute()
    {
        if (!recompute)
            return;
        int count = agentDataArray.Length;
        if (count == 0)
            return;
        m_CurrentNodeIndex = 0;
        BuildTreeRecursive(0, count);
    }

    private void BuildTreeRecursive(int begin, int end)
    {
        int nodeIndex = m_CurrentNodeIndex++;
        KDTreeNode node = kdTreeNodeArray[nodeIndex];

        node.begin = begin;
        node.end = end;
        node.left = -1;
        node.right = -1;

        float2 pos = agentDataArray[begin].Position2D;
        node.minX = node.maxX = pos.x;
        node.minY = node.maxY = pos.y;

        for (int i = begin + 1; i < end; i++)
        {
            pos = agentDataArray[i].Position2D;
            node.minX = math.min(node.minX, pos.x);
            node.maxX = math.max(node.maxX, pos.x);
            node.minY = math.min(node.minY, pos.y);
            node.maxY = math.max(node.maxY, pos.y);
        }

        kdTreeNodeArray[nodeIndex] = node;

        if (end - begin > maxLeavesCount)
        {
            bool isVertical = node.maxX - node.minX > node.maxY - node.minY;
            float splitValue = isVertical ? (node.minX + node.maxX) * 0.5f : (node.minY + node.maxY) * 0.5f;

            int left = begin;
            int right = end;
            while (left < right)
            {
                while (left < right && (isVertical
                           ? agentDataArray[left].Position2D.x < splitValue
                           : agentDataArray[left].Position2D.y < splitValue))
                {
                    left++;
                }
                while (left < right && (isVertical
                           ? agentDataArray[right - 1].Position2D.x >= splitValue
                           : agentDataArray[right - 1].Position2D.y >= splitValue))
                {
                    right--;
                }
                if (left < right)
                {
                    (agentDataArray[left], agentDataArray[right - 1]) =
                        (agentDataArray[right - 1], agentDataArray[left]);
                    left++;
                    right--;
                }
            }

            if (left == begin || left == end)
                return;

            int leftChildIndex = m_CurrentNodeIndex;
            BuildTreeRecursive(begin, left);
            int rightChildIndex = m_CurrentNodeIndex;
            BuildTreeRecursive(left, end);

            node = kdTreeNodeArray[nodeIndex];
            node.left = leftChildIndex;
            node.right = rightChildIndex;
            kdTreeNodeArray[nodeIndex] = node;
        }
    }
}