using Unity.Collections;

namespace ORCA
{
    public struct KDTree
    {
        private struct AgentTreeNode
        {
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

        [ReadOnly] public NativeArray<Agent> agents;
        [ReadOnly] private NativeArray<AgentTreeNode> agentTree;
    }
}