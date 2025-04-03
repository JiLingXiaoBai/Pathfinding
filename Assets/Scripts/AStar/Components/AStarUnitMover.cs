using Unity.Entities;
using Unity.Mathematics;

public struct AStarUnitMover : IComponentData
{
    public Random random;
    public float speed;
}