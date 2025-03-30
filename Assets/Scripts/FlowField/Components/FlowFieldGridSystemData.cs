using Unity.Collections;
using Unity.Entities;

public struct FlowFieldGridSystemData : IComponentData
{
    public NativeList<Entity> gridMapList;
}