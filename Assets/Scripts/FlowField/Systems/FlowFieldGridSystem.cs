using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;


partial struct FlowFieldGridSystem : ISystem
{
    private bool m_Initialized;
    private const int FLOW_FIELD_MAP_COUNT = 64;
    public const int WALL_COST = ushort.MaxValue;

    private Entity m_GridSystemDataEntity;

    private Entity m_GridMapEntityPrefab;
    private Entity m_GridNodeEntityPrefab;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<FlowFieldTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!m_Initialized)
        {
            Initialize(ref state);
            m_Initialized = true;
        }
        var gridSystemData = SystemAPI.GetComponentRW<FlowFieldGridSystemData>(m_GridSystemDataEntity);
        var mapCount = gridSystemData.ValueRO.gridMapList.Length;

        foreach ((RefRW<FlowFieldPathRequest> request,
                     EnabledRefRW<FlowFieldPathRequest> requestEnabled,
                     RefRW<FlowFieldFollower> follower,
                     EnabledRefRW<FlowFieldFollower> followerEnabled) in SystemAPI
                     .Query<RefRW<FlowFieldPathRequest>, EnabledRefRW<FlowFieldPathRequest>, RefRW<FlowFieldFollower>,
                         EnabledRefRW<FlowFieldFollower>>())
        {
            requestEnabled.ValueRW = false;
            int2 targetGridPosition = Pathfinding2DUtils.GetGridPosition(request.ValueRO.targetPosition);
            bool alreadyCalculatedPath = false;
            int unoccupiedMapIndex = -1;

            for (int i = 0; i < mapCount; i++)
            {
                var gridMap = SystemAPI.GetComponentRW<FlowFieldGridMap>(gridSystemData.ValueRO.gridMapList[i]);
                if (gridMap.ValueRO.targetGridPosition.Equals(targetGridPosition))
                {
                    gridMap.ValueRW.referenceCount++;
                    follower.ValueRW.targetPosition = request.ValueRO.targetPosition;
                    follower.ValueRW.mapIndex = i;
                    followerEnabled.ValueRW = true;
                    alreadyCalculatedPath = true;
                    break;
                }

                if (gridMap.ValueRO.referenceCount == 0 && unoccupiedMapIndex == -1)
                {
                    unoccupiedMapIndex = i;
                }
            }

            if (alreadyCalculatedPath) continue;

            if (unoccupiedMapIndex == -1)
            {
                unoccupiedMapIndex = mapCount;
                var gridMapEntity = CreateGridMapEntity(ref state, unoccupiedMapIndex);
                gridSystemData.ValueRW.gridMapList.Add(gridMapEntity);
            }
            follower.ValueRW.targetPosition = request.ValueRO.targetPosition;
            follower.ValueRW.mapIndex = unoccupiedMapIndex;
            followerEnabled.ValueRW = true;
            var unoccupiedGridMap =
                SystemAPI.GetComponentRW<FlowFieldGridMap>(gridSystemData.ValueRO.gridMapList[unoccupiedMapIndex]);
            unoccupiedGridMap.ValueRW.referenceCount++;


            PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
            InitializeGridJob initializeGridJob = new InitializeGridJob
            {
                mapIndex = unoccupiedMapIndex,
                targetGridPosition = targetGridPosition,
                collisionWorld = collisionWorld,
                collisionFilterWall = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << Pathfinding2DUtils.PATHFINDING_WALLS,
                    GroupIndex = 0
                }
            };
            JobHandle initializeGridJobHandle = initializeGridJob.ScheduleParallel(state.Dependency);
            initializeGridJobHandle.Complete();

            NativeArray<RefRW<FlowFieldGridNode>> gridNodeNativeArray =
                new NativeArray<RefRW<FlowFieldGridNode>>(Pathfinding2DUtils.NODES_COUNT, Allocator.Temp);
            for (int x = 0; x < Pathfinding2DUtils.WIDTH; x++)
            {
                for (int y = 0; y < Pathfinding2DUtils.HEIGHT; y++)
                {
                    int index = Pathfinding2DUtils.GetIndex(x, y);
                    Entity gridNodeEntity = unoccupiedGridMap.ValueRO.gridNodeArray[index];
                    RefRW<FlowFieldGridNode> gridNode = SystemAPI.GetComponentRW<FlowFieldGridNode>(gridNodeEntity);
                    gridNodeNativeArray[index] = gridNode;
                }
            }

            NativeQueue<RefRW<FlowFieldGridNode>> gridNodeOpenQueue =
                new NativeQueue<RefRW<FlowFieldGridNode>>(Allocator.Temp);
            RefRW<FlowFieldGridNode> targetGridNode =
                gridNodeNativeArray[Pathfinding2DUtils.GetIndex(targetGridPosition)];
            gridNodeOpenQueue.Enqueue(targetGridNode);

            int safety = 1024;
            while (gridNodeOpenQueue.Count > 0)
            {
                safety--;
                if (safety < 0) break;

                var currentGridNode = gridNodeOpenQueue.Dequeue();
                var neighbourGridNodeList = GetNeighbourGridNodeList(currentGridNode, gridNodeNativeArray);
                foreach (var neighbourGridNode in neighbourGridNodeList)
                {
                    if (neighbourGridNode.ValueRO.cost == WALL_COST)
                    {
                        continue;
                    }

                    uint newBestCost = currentGridNode.ValueRO.bestCost + neighbourGridNode.ValueRO.cost;
                    if (newBestCost < neighbourGridNode.ValueRO.bestCost)
                    {
                        neighbourGridNode.ValueRW.bestCost = newBestCost;
                        neighbourGridNode.ValueRW.dir = Pathfinding2DUtils.CalculateVector(neighbourGridNode.ValueRO.x,
                            neighbourGridNode.ValueRO.y, currentGridNode.ValueRO.x, currentGridNode.ValueRO.y);
                        gridNodeOpenQueue.Enqueue(neighbourGridNode);
                    }
                }
                neighbourGridNodeList.Dispose();
            }
            gridNodeOpenQueue.Dispose();
            gridNodeNativeArray.Dispose();
            unoccupiedGridMap.ValueRW.targetGridPosition = targetGridPosition;
        }
    }


    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        var gridSystemData = SystemAPI.GetComponentRW<FlowFieldGridSystemData>(m_GridSystemDataEntity);
        for (int i = 0; i < gridSystemData.ValueRO.gridMapList.Length; i++)
        {
            var gridMap = SystemAPI.GetComponentRW<FlowFieldGridMap>(gridSystemData.ValueRO.gridMapList[i]);
            gridMap.ValueRW.gridNodeArray.Dispose();
        }

        gridSystemData.ValueRW.gridMapList.Dispose();
    }

    private void Initialize(ref SystemState state)
    {
        var gridSystemDataEntity = state.EntityManager.CreateEntity();
        m_GridNodeEntityPrefab = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<FlowFieldGridNode>(m_GridNodeEntityPrefab);
        m_GridMapEntityPrefab = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<FlowFieldGridMap>(m_GridMapEntityPrefab);

        var gridMapList = new NativeList<Entity>(FLOW_FIELD_MAP_COUNT, Allocator.Persistent);

        for (int i = 0; i < FLOW_FIELD_MAP_COUNT; i++)
        {
            var gridMapEntity = CreateGridMapEntity(ref state, i);
            gridMapList.Add(gridMapEntity);
        }

        state.EntityManager.AddComponentData(gridSystemDataEntity, new FlowFieldGridSystemData
        {
            gridMapList = gridMapList
        });

        m_GridSystemDataEntity = gridSystemDataEntity;
    }

    private Entity CreateGridMapEntity(ref SystemState state, int mapIndex)
    {
        FlowFieldGridMap gridMap = new FlowFieldGridMap
        {
            gridNodeArray =
                new NativeArray<Entity>(Pathfinding2DUtils.NODES_COUNT, Allocator.Persistent),
            referenceCount = 0
        };

        state.EntityManager.Instantiate(m_GridNodeEntityPrefab, gridMap.gridNodeArray);

        for (int x = 0; x < Pathfinding2DUtils.WIDTH; x++)
        {
            for (int y = 0; y < Pathfinding2DUtils.HEIGHT; y++)
            {
                var index = Pathfinding2DUtils.GetIndex(x, y);
                FlowFieldGridNode gridNode = new FlowFieldGridNode
                {
                    x = (ushort)x,
                    y = (ushort)y,
                    mapIndex = (ushort)mapIndex,
                };
                SystemAPI.SetComponent(gridMap.gridNodeArray[index], gridNode);
            }
        }
        var gridMapEntity = state.EntityManager.Instantiate(m_GridMapEntityPrefab);
        SystemAPI.SetComponent(gridMapEntity, gridMap);
        return gridMapEntity;
    }

    private static NativeList<RefRW<FlowFieldGridNode>> GetNeighbourGridNodeList(
        RefRW<FlowFieldGridNode> currentGridNode,
        NativeArray<RefRW<FlowFieldGridNode>> gridNodeNativeArray)
    {
        NativeList<RefRW<FlowFieldGridNode>> neighbourGridNodeList =
            new NativeList<RefRW<FlowFieldGridNode>>(Allocator.Temp);
        int gridNodeX = currentGridNode.ValueRO.x;
        int gridNodeY = currentGridNode.ValueRO.y;

        int2 positionLeft = new int2(gridNodeX - 1, gridNodeY + 0);
        int2 positionRight = new int2(gridNodeX + 1, gridNodeY + 0);
        int2 positionUp = new int2(gridNodeX + 0, gridNodeY + 1);
        int2 positionDown = new int2(gridNodeX + 0, gridNodeY - 1);

        int2 positionLowerLeft = new int2(gridNodeX - 1, gridNodeY - 1);
        int2 positionLowerRight = new int2(gridNodeX + 1, gridNodeY - 1);
        int2 positionUpperLeft = new int2(gridNodeX - 1, gridNodeY + 1);
        int2 positionUpperRight = new int2(gridNodeX + 1, gridNodeY + 1);

        if (Pathfinding2DUtils.IsValidGridPosition(positionLeft))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[Pathfinding2DUtils.GetIndex(positionLeft)]);
        }
        if (Pathfinding2DUtils.IsValidGridPosition(positionRight))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[Pathfinding2DUtils.GetIndex(positionRight)]);
        }
        if (Pathfinding2DUtils.IsValidGridPosition(positionUp))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[Pathfinding2DUtils.GetIndex(positionUp)]);
        }
        if (Pathfinding2DUtils.IsValidGridPosition(positionDown))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[Pathfinding2DUtils.GetIndex(positionDown)]);
        }
        if (Pathfinding2DUtils.IsValidGridPosition(positionLowerLeft))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[Pathfinding2DUtils.GetIndex(positionLowerLeft)]);
        }
        if (Pathfinding2DUtils.IsValidGridPosition(positionLowerRight))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[Pathfinding2DUtils.GetIndex(positionLowerRight)]);
        }
        if (Pathfinding2DUtils.IsValidGridPosition(positionUpperLeft))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[Pathfinding2DUtils.GetIndex(positionUpperLeft)]);
        }
        if (Pathfinding2DUtils.IsValidGridPosition(positionUpperRight))
        {
            neighbourGridNodeList.Add(gridNodeNativeArray[Pathfinding2DUtils.GetIndex(positionUpperRight)]);
        }

        return neighbourGridNodeList;
    }

    public static bool IsWall(FlowFieldGridNode gridNode)
    {
        return gridNode.cost == WALL_COST;
    }
}

[BurstCompile]
public partial struct InitializeGridJob : IJobEntity
{
    [ReadOnly] public int mapIndex;
    [ReadOnly] public int2 targetGridPosition;
    [ReadOnly] public CollisionWorld collisionWorld;
    [ReadOnly] public CollisionFilter collisionFilterWall;

    private void Execute(ref FlowFieldGridNode gridNode)
    {
        if (gridNode.mapIndex != mapIndex)
        {
            return;
        }

        gridNode.dir = new float2(0, 1);
        if (gridNode.x == targetGridPosition.x && gridNode.y == targetGridPosition.y)
        {
            gridNode.cost = 0;
            gridNode.bestCost = 0;
        }
        else
        {
            NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.TempJob);
            gridNode.cost = 1;
            gridNode.bestCost = uint.MaxValue;

            if (collisionWorld.OverlapSphere(
                    Pathfinding2DUtils.GetWorldCenterPosition(gridNode.x, gridNode.y),
                    Pathfinding2DUtils.NODE_SIZE_HALF,
                    ref distanceHitList,
                    collisionFilterWall))
            {
                gridNode.cost = FlowFieldGridSystem.WALL_COST;
            }
            distanceHitList.Dispose();
        }
    }
}