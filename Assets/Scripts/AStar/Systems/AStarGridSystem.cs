using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;

partial struct AStarGridSystem : ISystem
{
    private NativeList<int2> m_WallPositions;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<AStarTag>();
        m_WallPositions = new NativeList<int2>(Pathfinding2DUtils.NODES_COUNT, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_WallPositions.Clear();
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
        UpdateWallGridJob updateWallGridJob = new UpdateWallGridJob
        {
            wallPositions = m_WallPositions.AsParallelWriter(),
            collisionWorld = collisionWorld,
            collisionFilterWall = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << Pathfinding2DUtils.PATHFINDING_WALLS,
                GroupIndex = 0
            }
        };
        var updateWallGridJobHandle = updateWallGridJob.Schedule(Pathfinding2DUtils.NODES_COUNT, 64, state.Dependency);
        updateWallGridJobHandle.Complete();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        m_WallPositions.Dispose();
    }

    [BurstCompile]
    partial struct UpdateWallGridJob : IJobParallelFor
    {
        public NativeList<int2>.ParallelWriter wallPositions;
        [ReadOnly] public CollisionWorld collisionWorld;
        [ReadOnly] public CollisionFilter collisionFilterWall;

        public void Execute(int index)
        {
            NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.TempJob);
            int2 gridPosition = Pathfinding2DUtils.GetGridPositionFromIndex(index);
            if (collisionWorld.OverlapSphere(
                    Pathfinding2DUtils.GetWorldCenterPosition(gridPosition.x, gridPosition.y),
                    Pathfinding2DUtils.NODE_SIZE_HALF,
                    ref distanceHitList,
                    collisionFilterWall))
            {
                wallPositions.AddNoResize(gridPosition);
            }
            distanceHitList.Dispose();
        }
    }
}