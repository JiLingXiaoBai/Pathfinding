using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct FlowFieldGridMap : IComponentData
{
    public NativeArray<Entity> gridNodeArray;
    public int2 targetGridPosition;
    public int referenceCount;
}