using Unity.Burst;

[BurstCompile]
public struct AgentTreeNode
{
    public const int MAX_LEAF_SIZE = 10;

    internal int begin;
    internal int end;
    internal int left;
    internal int right;

    #region range of KDTreeNode

    internal float maxX;
    internal float minX;
    internal float maxY;
    internal float minY;

    #endregion
}