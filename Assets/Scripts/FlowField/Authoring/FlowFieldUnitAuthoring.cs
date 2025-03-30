using Unity.Entities;
using UnityEngine;

public class FlowFieldUnitAuthoring : MonoBehaviour
{
    private class FlowFieldUnitMoverBaker : Baker<FlowFieldUnitAuthoring>
    {
        public override void Bake(FlowFieldUnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new FlowFieldUnitMover
            {
                random = new Unity.Mathematics.Random(123),
                speed = 10,
            });
            AddComponent(entity, new FlowFieldFollower());
            SetComponentEnabled<FlowFieldFollower>(entity,false);
            AddComponent(entity, new FlowFieldPathRequest());
            SetComponentEnabled<FlowFieldPathRequest>(entity,false);
        }
    }
}