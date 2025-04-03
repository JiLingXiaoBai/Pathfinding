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
    private ComponentLookup<AStarPathRequest> m_RequestLookup;
    private ComponentLookup<AStarFollower> m_FollowerLookup;
    private bool m_InitWallGrid;


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
            return result;
        }

        public bool Equals(AStarGridNodeCost other)
        {
            return currentPos.Equals(other.currentPos);
        }

        public override int GetHashCode()
        {
            return currentPos.GetHashCode();
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<AStarTag>();
        m_WallPositions = new NativeList<int2>(Pathfinding2DUtils.NODES_COUNT, Allocator.Persistent);
        m_RequestLookup = state.GetComponentLookup<AStarPathRequest>();
        m_FollowerLookup = state.GetComponentLookup<AStarFollower>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_RequestLookup.Update(ref state);
        m_FollowerLookup.Update(ref state);
        if (!m_InitWallGrid)
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
            var updateWallGridJobHandle =
                updateWallGridJob.Schedule(Pathfinding2DUtils.NODES_COUNT, 64, state.Dependency);
            updateWallGridJobHandle.Complete();
            m_InitWallGrid = true;
        }

        FindPathJob findPathJob = new FindPathJob
        {
            wallPositions = m_WallPositions,
            requestLookup = m_RequestLookup,
            followerLookup = m_FollowerLookup
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
    [WithPresent(typeof(AStarFollower))]
    partial struct FindPathJob : IJobEntity
    {
        [ReadOnly] public NativeList<int2> wallPositions;
        [NativeDisableParallelForRestriction] public ComponentLookup<AStarPathRequest> requestLookup;
        [NativeDisableParallelForRestriction] public ComponentLookup<AStarFollower> followerLookup;

        private void Execute(in LocalTransform localTransform, DynamicBuffer<AStarPathNode> pathNodes, Entity entity)
        {
            if (requestLookup.IsComponentEnabled(entity) == false)
            {
                return;
            }
            pathNodes.Clear();
            var request = requestLookup[entity];
            var follower = followerLookup[entity];
            follower.index = 0;
            follower.targetPosition = request.targetPosition;
            followerLookup[entity] = follower;
            int2 startPos = Pathfinding2DUtils.GetGridPosition(localTransform.Position);
            int2 endPos = Pathfinding2DUtils.GetGridPosition(request.targetPosition);

            NativeList<int2> result = new NativeList<int2>(Allocator.TempJob);
            NativeBinaryHeap<AStarGridNodeCost> openList =
                new NativeBinaryHeap<AStarGridNodeCost>(NativeBinaryHeapType.Minimum, Pathfinding2DUtils.NODES_COUNT,
                    Allocator.TempJob);
            NativeHashMap<int2, AStarGridNodeCost> closeList =
                new NativeHashMap<int2, AStarGridNodeCost>(Pathfinding2DUtils.NODES_COUNT, Allocator.TempJob);

            openList.Add(new AStarGridNodeCost(startPos, startPos));
            AStarGridNodeCost currentNode = new AStarGridNodeCost(startPos, startPos);
            bool foundPath = false;
            while (openList.Count > 0)
            {
                currentNode = openList.RemoveFirst();
                if (closeList.ContainsKey(currentNode.currentPos))
                {
                    continue;
                }
                closeList.Add(currentNode.currentPos, currentNode);
                if (currentNode.currentPos.Equals(endPos))
                {
                    foundPath = true;
                    break;
                }

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }

                        int2 newPos = new int2(currentNode.currentPos.x + x, currentNode.currentPos.y + y);

                        if (Pathfinding2DUtils.IsValidGridPosition(newPos))
                        {
                            if (wallPositions.Contains(newPos) || closeList.TryGetValue(newPos, out _))
                                continue;

                            AStarGridNodeCost newCost = new AStarGridNodeCost(newPos, currentNode.currentPos);
                            int newGCost = currentNode.gCost + NodeDistance(currentNode.currentPos, newPos);
                            newCost.gCost = newGCost;
                            newCost.hCost = NodeDistance(newPos, endPos);

                            int index = openList.IndexOf(newCost);
                            if (index >= 0)
                            {
                                if (newGCost < openList[index].gCost)
                                {
                                    openList.RemoveAt(index);
                                    openList.Add(newCost);
                                }
                            }
                            else
                            {
                                openList.Add(newCost);
                            }
                        }
                    }
                }
            }
            if (foundPath)
            {
                while (!currentNode.currentPos.Equals(currentNode.originPos))
                {
                    result.Add(currentNode.currentPos);
                    if (!closeList.TryGetValue(currentNode.originPos, out var next))
                        break;
                    currentNode = next;
                }


                for (int i = result.Length - 1; i >= 0; i--)
                {
                    pathNodes.Add(new AStarPathNode
                    {
                        position = result[i]
                    });
                }
            }

            requestLookup.SetComponentEnabled(entity, false);
            followerLookup.SetComponentEnabled(entity, true);
            result.Dispose();
            openList.Dispose();
            closeList.Dispose();
        }

        private int NodeDistance(int2 a, int2 b)
        {
            int2 temp = a - b;
            int tempX = math.abs(temp.x);
            int tempY = math.abs(temp.y);
            if (tempX > tempY)
                return tempY * 14 + (tempX - tempY) * 10;
            else
                return tempX * 14 + (tempY - tempX) * 10;
        }
    }
}