using Unity.Entities;
using UnityEngine;

public class AStarUnitAuthoring : MonoBehaviour
{
    [SerializeField] private uint randomSeed;
    private class AStarUnitBaker : Baker<AStarUnitAuthoring>
    {
        public override void Bake(AStarUnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AStarFollower());
            SetComponentEnabled<AStarFollower>(entity,false);
            AddComponent(entity, new AStarPathRequest());
            SetComponentEnabled<AStarPathRequest>(entity,false);
            AddBuffer<AStarPathNode>(entity);
            AddComponent(entity, new AStarUnitMover
            {
                random = new Unity.Mathematics.Random(authoring.randomSeed),
                speed = 30f,
            });
        }
    }
}