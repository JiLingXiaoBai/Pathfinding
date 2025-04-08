using Unity.Entities;
using Unity.Mathematics;

public struct AgentData : IComponentData, IKDTreeItem
{
    public int index;
    public float2 position;
    public float2 prefVelocity;
    public float2 velocity;
    public float radius;
    public float weight;
    public float maxSpeed;
    public int maxNeighbors;
    public float neighborDist;
    public float timeHorizon;
    public float timeHorizonObst;
    public float2 targetPos;
    public float2 Position2D => position;
}