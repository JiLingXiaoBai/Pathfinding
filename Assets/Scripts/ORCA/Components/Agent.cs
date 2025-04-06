using Unity.Entities;
using Unity.Mathematics;

public struct Agent : IComponentData
{
    public int id;
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
}