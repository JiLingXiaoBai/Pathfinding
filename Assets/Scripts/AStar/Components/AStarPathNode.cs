using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(1024)]
public struct AStarPathNode : IBufferElementData
{
    public int2 position;
}