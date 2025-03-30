using Unity.Entities;
using Unity.Mathematics;

public struct FlowFieldGridNode : IComponentData
{
    public ushort x;
    public ushort y;
    public ushort mapIndex;
    public ushort cost;
    public uint bestCost;
    public float2 dir;
}