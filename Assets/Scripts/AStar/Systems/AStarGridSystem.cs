using Unity.Burst;
using Unity.Entities;

partial struct AStarGridSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AStarTag>();
    }
    
}