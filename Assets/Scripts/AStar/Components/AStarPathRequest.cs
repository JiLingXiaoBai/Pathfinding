    using Unity.Entities;
    using Unity.Mathematics;

    public struct AStarPathRequest : IComponentData, IEnableableComponent
    {
        public float2 targetPosition;
    }