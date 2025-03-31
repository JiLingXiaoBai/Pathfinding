using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct FlowFieldRandomWalkingSystem : ISystem
{
    public const float REACHED_TARGET_POSITION_DISTANCE_SQ = 2f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FlowFieldTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((RefRW<FlowFieldUnitMover> unitMover,
                     RefRO<LocalTransform> localTransform, RefRW<FlowFieldPathRequest> request,
                     EnabledRefRW<FlowFieldPathRequest> requestEnabled)
                 in SystemAPI
                     .Query<RefRW<FlowFieldUnitMover>, RefRO<LocalTransform>,
                         RefRW<FlowFieldPathRequest>,
                         EnabledRefRW<FlowFieldPathRequest>>().WithDisabled<FlowFieldPathRequest>())
        {
            var targetPos = request.ValueRO.targetPosition;
            if (math.distancesq(localTransform.ValueRO.Position, new float3(targetPos.x, 0, targetPos.y)) <
                REACHED_TARGET_POSITION_DISTANCE_SQ)
            {
                Random random = unitMover.ValueRO.random;
                float3 randomPos = new float3(random.NextFloat(0, Pathfinding2DUtils.WIDTH), 0,
                    random.NextFloat(0f, Pathfinding2DUtils.HEIGHT));

                unitMover.ValueRW.targetPos = randomPos;
                unitMover.ValueRW.random = random;
                requestEnabled.ValueRW = true;
                request.ValueRW.targetPosition =
                    new float2(unitMover.ValueRO.targetPos.x, unitMover.ValueRO.targetPos.z);
            }
        }
    }
}