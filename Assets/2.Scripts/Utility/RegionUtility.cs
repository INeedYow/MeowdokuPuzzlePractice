using UnityEngine;

public static class RegionUtility
{
    const int MaxRegionCount = 26;


    public static char GetIdFromIndex(int index)
    {
        return (char)('a' + index);
    }
    public static bool TryGetIdFromIndex(int index, out char id)
    {
        id = default;

        if (index < 0 || index >= MaxRegionCount)
            return false;

        id = (char)('a' + index);
        return true;
    }

    public static bool TryGetIndexFromId(char id, out int index)
    {
        index = default;

        id = char.ToLower(id);  //

        if (id < 'a' || id > 'z')
            return false;

        index = (int)(id - 'a');
        return true;
    }

    public static char GetRegionId(char[,] board, Vector2Int pos)
    {
        return char.ToLower(board[pos.y, pos.x]);
    }
}
