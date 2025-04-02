    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct AStarFollower : IComponentData, IEnableableComponent
    {
        public float2 targetPosition;
        public int index;
    }