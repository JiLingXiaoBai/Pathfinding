using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct FlowFieldUnitMoverSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FlowFieldTag>();
        state.RequireForUpdate<FlowFieldGridSystemData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        FlowFieldGridSystemData gridSystemData = SystemAPI.GetSingleton<FlowFieldGridSystemData>();

        foreach ((RefRO<LocalTransform> localTransform, RefRW<FlowFieldFollower> flowFieldFollower,
                     RefRW<FlowFieldUnitMover> unitMover, Entity entity) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRW<FlowFieldFollower>, RefRW<FlowFieldUnitMover>>()
                     .WithPresent<FlowFieldFollower>()
                     .WithEntityAccess())
        {
            int2 gridPosition = Pathfinding2DUtils.GetGridPosition(localTransform.ValueRO.Position);
            int index = Pathfinding2DUtils.GetIndex(gridPosition);
            var gridMap =
                SystemAPI.GetComponentRW<FlowFieldGridMap>(
                    gridSystemData.gridMapList[flowFieldFollower.ValueRO.mapIndex]);
            var gridNodeEntity = gridMap.ValueRO.gridNodeArray[index];
            var gridNode = SystemAPI.GetComponent<FlowFieldGridNode>(gridNodeEntity);
            float2 gridNodeMoveVector = gridNode.dir;

            if (FlowFieldGridSystem.IsWall(gridNode))
            {
                gridNodeMoveVector = flowFieldFollower.ValueRO.lastMoveDir;
            }
            else
            {
                flowFieldFollower.ValueRW.lastMoveDir = gridNode.dir;
            }

            var moveDir = new float3(gridNodeMoveVector.x, 0, gridNodeMoveVector.y);
            unitMover.ValueRW.targetPos =
                Pathfinding2DUtils.GetWorldCenterPosition(gridPosition.x, gridPosition.y) +
                moveDir * Pathfinding2DUtils.NODE_SIZE_DOUBLE;

            var targetPos = new float3(flowFieldFollower.ValueRO.targetPosition.x, 0,
                flowFieldFollower.ValueRO.targetPosition.y);
            if (math.distancesq(localTransform.ValueRO.Position, targetPos) <
                Pathfinding2DUtils.NODE_SIZE_SQR)
            {
                UnityEngine.Debug.Log("FlowFieldUnitMoverSystem: Unit reached target position");
                gridMap.ValueRW.referenceCount--;
                unitMover.ValueRW.targetPos = localTransform.ValueRO.Position;
                SystemAPI.SetComponentEnabled<FlowFieldFollower>(entity, false);
            }
        }

        var unitMoverJob = new UnitMoverJob
        {
            deltaTime = SystemAPI.Time.DeltaTime
        };
        unitMoverJob.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct UnitMoverJob : IJobEntity
    {
        public float deltaTime;

        private void Execute(ref LocalTransform localTransform, ref FlowFieldUnitMover unitMover)
        {
            float3 moveDirection = unitMover.targetPos - localTransform.Position;

            float reachedTargetDistanceSq = FlowFieldRandomWalkingSystem.REACHED_TARGET_POSITION_DISTANCE_SQ;
            if (math.lengthsq(moveDirection) <= reachedTargetDistanceSq)
            {
                return;
            }

            moveDirection = math.normalize(moveDirection);
            localTransform.Position += moveDirection * unitMover.speed * deltaTime;
        }
    }
}


