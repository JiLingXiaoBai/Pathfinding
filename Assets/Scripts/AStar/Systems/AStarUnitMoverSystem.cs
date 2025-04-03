using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct AStarUnitMoverSystem : ISystem
{
    private ComponentLookup<AStarFollower> m_FollowerLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AStarTag>();
        m_FollowerLookup = state.GetComponentLookup<AStarFollower>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_FollowerLookup.Update(ref state);
        UnitMoverJob unitMoverJob = new UnitMoverJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            followerLookup = m_FollowerLookup
        };
        var jobHandle = unitMoverJob.ScheduleParallel(state.Dependency);
        jobHandle.Complete();
    }


    [BurstCompile]
    public partial struct UnitMoverJob : IJobEntity
    {
        [NativeDisableParallelForRestriction] public ComponentLookup<AStarFollower> followerLookup;
        [ReadOnly] public float deltaTime;

        private void Execute(ref LocalTransform localTransform, ref AStarUnitMover unitMover,
            in DynamicBuffer<AStarPathNode> pathNodes, Entity entity)
        {
            var follower = followerLookup[entity];
            if (follower.index >= pathNodes.Length)
            {
                followerLookup.SetComponentEnabled(entity, false);
                return;
            }

            var targetPos = pathNodes[follower.index].position;
            // var worldTargetPos = Pathfinding2DUtils.GetWorldCenterPosition(targetPos.x, targetPos.y);
            // float3 moveDir = worldTargetPos - localTransform.Position;

            // if (math.lengthsq(moveDir) <= 0.0001f)
            // {
            //     follower.index += 1;
            //     followerLookup[entity] = follower;
            // }
            // moveDir = math.normalize(moveDir);
            // localTransform.Position += moveDir * unitMover.speed * deltaTime;
            localTransform.Position = new float3(targetPos.x, 0f, targetPos.y);
            follower.index += 1;
            followerLookup[entity] = follower;
        }
    }
}