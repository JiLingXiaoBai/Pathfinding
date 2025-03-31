using Unity.Entities;
using UnityEngine;

public class PathfindingTypeController : MonoBehaviour
{
    [SerializeField] private Pathfinding2DType type;

    private class PathfindingTypeControllerBaker : Baker<PathfindingTypeController>
    {
        public override void Bake(PathfindingTypeController authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            if (authoring.type == Pathfinding2DType.FlowField)
            {
                AddComponent(entity, new FlowFieldTag());
            }
            else if (authoring.type == Pathfinding2DType.AStar)
            {
                AddComponent(entity, new AStarTag());
            }
        }
    }
}