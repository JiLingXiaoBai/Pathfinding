using Unity.Entities;
using Unity.Mathematics;

public struct FlowFieldPathRequest : IComponentData, IEnableableComponent
{
    public float2 targetPosition;
}