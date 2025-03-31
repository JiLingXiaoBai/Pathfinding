using Unity.Entities;

public struct AStarGridNode : IComponentData
{
    public int x;
    public int y;
    
    public int gCost;
    public int hCost;
    public int fCost;
}