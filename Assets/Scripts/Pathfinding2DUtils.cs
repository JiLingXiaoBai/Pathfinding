using Unity.Mathematics;

public struct Pathfinding2DUtils
{
    public const int WIDTH = 256;
    public const int HEIGHT = 256;
    public const int NODES_COUNT = WIDTH * HEIGHT;
    public const float NODE_SIZE = 1f;
    public const float NODE_SIZE_SQR = NODE_SIZE * NODE_SIZE;
    public const float NODE_SIZE_DOUBLE = NODE_SIZE * 2f;
    public const float NODE_SIZE_HALF = NODE_SIZE * 0.5f;
    public const int PATHFINDING_WALLS = 6;

    public static int GetIndex(int x, int y)
    {
        return x + y * WIDTH;
    }

    public static int GetIndex(int2 gridPos)
    {
        return gridPos.x + gridPos.y * WIDTH;
    }

    public static int2 GetGridPositionFromIndex(int index)
    {
        int y = index / WIDTH;
        int x = index % WIDTH;
        return new int2(x, y);
    }

    public static int2 GetGridPosition(float2 worldPos)
    {
        return new int2((int)math.floor(worldPos.x / NODE_SIZE), (int)math.floor(worldPos.y / NODE_SIZE));
    }

    public static int2 GetGridPosition(float3 worldPos)
    {
        return new int2((int)math.floor(worldPos.x / NODE_SIZE), (int)math.floor(worldPos.z / NODE_SIZE));
    }

    public static float3 GetWorldCenterPosition(int x, int y)
    {
        return new float3(x * NODE_SIZE + NODE_SIZE_HALF, 0, y * NODE_SIZE + NODE_SIZE_HALF);
    }

    public static bool IsValidGridPosition(int2 gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < WIDTH &&
               gridPosition.y >= 0 && gridPosition.y < HEIGHT;
    }

    public static float2 CalculateVector(int fromX, int fromY, int toX, int toY)
    {
        return new float2(toX, toY) - new float2(fromX, fromY);
    }
}

public enum Pathfinding2DType
{
    FlowField,
    AStar,
}