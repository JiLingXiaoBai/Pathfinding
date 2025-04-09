using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
partial struct ORCASystem : ISystem
{
    private NativeArray<AgentData> m_AgentDataArray;
    private NativeArray<KDTreeNode> m_AgentTreeNodeArray;
}