using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct FlowFieldRandomWalkingSystem : ISystem
{
    public const float REACHED_TARGET_POSITION_DISTANCE_SQ = 2f;
    public const float DISTANCE_MAX = 100f;
    public const float DISTANCE_MIN = 10f;

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
                     .Query<RefRW<FlowFieldUnitMover>, RefRO<LocalTransform>, RefRW<FlowFieldPathRequest>,
                         EnabledRefRW<FlowFieldPathRequest>>().WithDisabled<FlowFieldPathRequest>())
        {
            if (math.distancesq(localTransform.ValueRO.Position, unitMover.ValueRO.targetPos) <
                REACHED_TARGET_POSITION_DISTANCE_SQ)
            {
                Random random = unitMover.ValueRO.random;
                float3 randomDirection = new float3(random.NextFloat(-1f, +1f), 0, random.NextFloat(-1f, +1f));
                randomDirection = math.normalize(randomDirection);

                unitMover.ValueRW.targetPos = localTransform.ValueRO.Position + randomDirection *
                    random.NextFloat(DISTANCE_MIN, DISTANCE_MAX);

                unitMover.ValueRW.random = random;
                requestEnabled.ValueRW = true;
                request.ValueRW.targetPosition =
                    new float2(unitMover.ValueRO.targetPos.x, unitMover.ValueRO.targetPos.z);
            }
        }
    }
}