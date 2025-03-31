using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FlowFieldUnitAuthoring : MonoBehaviour
{
    [SerializeField] private uint randomSeed;

    private class FlowFieldUnitMoverBaker : Baker<FlowFieldUnitAuthoring>
    {
        public override void Bake(FlowFieldUnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new FlowFieldUnitMover
            {
                random = new Unity.Mathematics.Random(authoring.randomSeed),
                speed = 30,
            });
            AddComponent(entity, new FlowFieldFollower());
            SetComponentEnabled<FlowFieldFollower>(entity, false);
            AddComponent(entity, new FlowFieldPathRequest()
            {
                targetPosition = new float2(authoring.transform.position.x, authoring.transform.position.z),
            });
            SetComponentEnabled<FlowFieldPathRequest>(entity, false);
        }
    }
}