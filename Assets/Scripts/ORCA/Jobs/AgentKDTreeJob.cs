using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct AgentKDTreeJob : IJob
{
    public NativeArray<AgentData> agentDataArray;
    public NativeArray<AgentTreeNode> agentTreeNodeArray;
    private int m_CurrentNodeIndex;

    public void Execute()
    {
        int count = agentDataArray.Length;
        if (count == 0)
            return;
        m_CurrentNodeIndex = 0;
        BuildTreeRecursive(0, count);
    }

    private void BuildTreeRecursive(int begin, int end)
    {
        int nodeIndex = m_CurrentNodeIndex++;
        AgentTreeNode node = agentTreeNodeArray[nodeIndex];

        node.begin = begin;
        node.end = end;
        node.left = -1;
        node.right = -1;

        float2 pos = agentDataArray[begin].position;
        node.minX = node.maxX = pos.x;
        node.minY = node.maxY = pos.y;

        for (int i = begin + 1; i < end; i++)
        {
            pos = agentDataArray[i].position;
            node.minX = math.min(node.minX, pos.x);
            node.maxX = math.max(node.maxX, pos.x);
            node.minY = math.min(node.minY, pos.y);
            node.maxY = math.max(node.maxY, pos.y);
        }

        agentTreeNodeArray[nodeIndex] = node;

        if (end - begin > AgentTreeNode.MAX_LEAF_SIZE)
        {
            bool isVertical = node.maxX - node.minX > node.maxY - node.minY;
            float splitValue = isVertical ? (node.minX + node.maxX) * 0.5f : (node.minY + node.maxY) * 0.5f;

            int left = begin;
            int right = end;
            while (left < right)
            {
                while (left < right && (isVertical
                           ? agentDataArray[left].position.x < splitValue
                           : agentDataArray[left].position.y < splitValue))
                {
                    left++;
                }
                while (left < right && (isVertical
                           ? agentDataArray[right - 1].position.x >= splitValue
                           : agentDataArray[right - 1].position.y >= splitValue))
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
            
            node = agentTreeNodeArray[nodeIndex];
            node.left = leftChildIndex;
            node.right = rightChildIndex;
            agentTreeNodeArray[nodeIndex] = node;
        }
    }
}