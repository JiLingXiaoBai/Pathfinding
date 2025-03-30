using Unity.Entities;
using Unity.Mathematics;

public struct FlowFieldFollower : IComponentData, IEnableableComponent
{
    public float2 targetPosition;
    public float2 lastMoveDir;
    public int mapIndex;
}