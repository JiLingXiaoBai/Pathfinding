using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

partial struct AStarRandomWalkingSystem : ISystem
{
    private ComponentLookup<AStarFollower> m_FollowerLookup;
    private ComponentLookup<AStarPathRequest> m_RequestLookup;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AStarTag>();
        m_FollowerLookup = state.GetComponentLookup<AStarFollower>();
        m_RequestLookup = state.GetComponentLookup<AStarPathRequest>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_FollowerLookup.Update(ref state);
        m_RequestLookup.Update(ref state);
        AStarRandomWalkingJob job = new AStarRandomWalkingJob
        {
            followerLookup = m_FollowerLookup,
            requestLookup = m_RequestLookup
        };
        var jobHandle = job.ScheduleParallel(state.Dependency);
        jobHandle.Complete();
    }


    [BurstCompile]
    partial struct AStarRandomWalkingJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<AStarFollower> followerLookup;
        [NativeDisableParallelForRestriction] public ComponentLookup<AStarPathRequest> requestLookup;

        private void Execute(ref AStarUnitMover unitMover, Entity entity)
        {
            if (!followerLookup.IsComponentEnabled(entity) && !requestLookup.IsComponentEnabled(entity))
            {
                var request = requestLookup[entity];
                Random random = unitMover.random;
                float2 randomPos = new float2(random.NextFloat(0f, Pathfinding2DUtils.WIDTH),
                    random.NextFloat(0f, Pathfinding2DUtils.HEIGHT));
                request.targetPosition = new float2(randomPos.x, randomPos.y);
                requestLookup[entity] = request;
                requestLookup.SetComponentEnabled(entity, true);
                unitMover.random = random;
            }
        }
    }
}