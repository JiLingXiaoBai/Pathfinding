using Unity.Entities;
using UnityEngine;

public class FlowFieldUnitMoverAuthoring : MonoBehaviour
{
    private class FlowFieldUnitMoverBaker : Baker<FlowFieldUnitMoverAuthoring>
    {
        public override void Bake(FlowFieldUnitMoverAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new FlowFieldUnitMover());
        }
    }
}
