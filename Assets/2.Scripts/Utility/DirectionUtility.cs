using UnityEngine;

public static class DirectionUtility
{
    public static readonly Vector2Int Up        = new Vector2Int(0, -1);
    public static readonly Vector2Int Down      = new Vector2Int(0, 1);
    public static readonly Vector2Int Left      = new Vector2Int(-1, 0);
    public static readonly Vector2Int Right     = new Vector2Int(1, 0);

    public static readonly Vector2Int UpLeft    = new Vector2Int(-1, -1);
    public static readonly Vector2Int UpRight   = new Vector2Int(1, -1);
    public static readonly Vector2Int DownLeft  = new Vector2Int(-1, 1);
    public static readonly Vector2Int DownRight = new Vector2Int(1, 1);

    /// <summary>
    /// 4방향 (상하좌우)
    /// </summary>
    public static readonly Vector2Int[] Directions4 = 
    { 
        Up, Down, Left, Right 
    };

    /// <summary>
    /// 8방향 (상하좌우 + 대각)
    /// </summary>
    public static readonly Vector2Int[] Directions8 = 
    { 
        Up, Down, Left, Right,
        UpLeft, UpRight, DownLeft, DownRight
    };

    /// <summary>
    /// 대각 4방향 (대각)
    /// </summary>
    public static readonly Vector2Int[] DiagonalDirections =
    {
        UpLeft, UpRight, DownLeft, DownRight
    };
}
