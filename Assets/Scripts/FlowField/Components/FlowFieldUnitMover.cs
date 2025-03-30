using Unity.Entities;
using Unity.Mathematics;

public struct FlowFieldUnitMover : IComponentData
{
    public Random random;
    public float3 targetPos;
    public float speed;
}