using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial struct AStarGridSystem : ISystem
{
    private NativeList<int2> m_WallPositions;
    
    
    public struct AStarGridNodeCost : IComparable<AStarGridNodeCost>, IEquatable<AStarGridNodeCost>
    {
        public int2 currentPos;
        public int2 originPos;
        public int gCost;
        public int hCost;

        public AStarGridNodeCost(int2 currentPos, int2 originPos)
        {
            this.currentPos = currentPos;
            this.originPos = originPos;
            this.gCost = 0;
            this.hCost = 0;
        }

        private int FCost => gCost + hCost;

        public int CompareTo(AStarGridNodeCost other)
        {
            int result = FCost.CompareTo(other.FCost);
            if (result == 0)
            {
                result = hCost.CompareTo(other.hCost);
            }
            return -result;
        }

        public bool Equals(AStarGridNodeCost other)
        {
            return this.currentPos.Equals(other.currentPos);
        }
    }

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

        FindPathJob findPathJob = new FindPathJob
        {
        };
        var findPathJobHandle = findPathJob.ScheduleParallel(state.Dependency);
        findPathJobHandle.Complete();
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

    [BurstCompile]
    partial struct FindPathJob : IJobEntity
    {
        private void Execute(in LocalTransform localTransform, in AStarPathRequest request, ref AStarFollower follower, EnabledRefRW<AStarFollower> followerEnabled)
        {
            // follower.pathPos = new NativeList<int2>(Allocator.TempJob);
            follower.targetPosition = request.targetPosition;
            follower.index = 0;
            followerEnabled.ValueRW = false;
            // follower.pathPos.Dispose();
        }
    }
}