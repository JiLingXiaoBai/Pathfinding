using Unity.Entities;
using UnityEngine;

public class AStarUnitAuthoring : MonoBehaviour
{
    private class AStarUnitBaker : Baker<AStarUnitAuthoring>
    {
        public override void Bake(AStarUnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AStarFollower());
            AddComponent(entity, new AStarPathRequest());
        }
    }
}