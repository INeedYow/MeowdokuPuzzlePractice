using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class TileColorGenerator : MonoBehaviour
{
    [Header("Debug Log")]
    [SerializeField] bool showLog = true;

    int totalCatCount;


    // 같은 알파벳은 같은 색을 의미
    // 알파벳 대문자는 해당 색의 고양이 위치를 의미
    // \0 (DefaultChar) : 아직 결정하지 않은 타일
    char[,] board;
    const char DefaultChar = '\0';
    const int MaxAlphabetCount = 26;


    static readonly Vector2Int Up      = new Vector2Int(0, 1);
    static readonly Vector2Int Down    = new Vector2Int(0, -1);
    static readonly Vector2Int Left    = new Vector2Int(-1, 0);
    static readonly Vector2Int Right   = new Vector2Int(1, 0);
    static readonly Vector2Int[] Directions = { Up, Down, Left, Right };

    public char[,] Generate(int catCount, List<Vector2Int> catPositions)
    {
        totalCatCount = catCount;
        board = new char[catCount, catCount];

        for (int i = 0; i < catPositions.Count; i++)
        {
            Vector2Int pos = catPositions[i];
            board[pos.y, pos.x] = char.ToUpper(GetAlphabetFromIndex(i));
        }

        for (int i = 0; i < catPositions.Count; i++)
        {
            DecideColorFromCat(catPositions[i], GetAlphabetFromIndex(i));
        }

        DecideEmptyTilesColor();

        DebugLog_Result();

        return board;
    }

    // 아이디어
    // 제일 위 고양이부터(순서가 색칠하는 영역에 영향이 있다면 무작위 순서 고려) 상하좌우 인접한 타일을 같은 색으로 칠하거나 포기
        // 한 타일을 여러번 결정하지 않기 위해 decided에 결정 여부 저장
    // 모든 방향에서 포기한 경우 다음 고양이에서 반복
    // 모든 고양이에서 타일 색칠 끝낸 경우, 아직 색이 정해지지 않은 타일은 인접한 색들 중에서 하나로 결정
    void DecideColorFromCat(Vector2Int catPosition, char alphabet)
    {
        bool[,] decided = new bool[totalCatCount, totalCatCount];
        List<Vector2Int> candidates = new List<Vector2Int>();

        foreach(var dir in Directions)
            candidates.Add(catPosition + dir);

        // Log
        if (showLog)
            Debug.Log($"DecideColor : {catPosition} '{alphabet}' 영역 결정 시작");

        while (candidates.Count > 0)
        {
            int randomIndex = Random.Range(0, candidates.Count);
            Vector2Int candidate = candidates[randomIndex];
            candidates.RemoveAt(randomIndex);

            if (TryColorAt(candidate, decided))
            {
                board[candidate.y, candidate.x] = alphabet;
                // Log
                if (showLog)
                    Debug.Log($"DecideColor : {candidate} 타일 '{alphabet}'로 결정");

                foreach (var dir in Directions)
                    candidates.Add(candidate + dir);
            }
        }
    }

    bool TryColorAt(Vector2Int position, bool[,] decided)
    {
        if (position.x < 0 || position.x >= board.GetLength(1)
            || position.y < 0 || position.y >= board.GetLength(0))
        {
            return false;
        }

        // 이미 색이 결정된 타일(고양이 타일 포함)
        if (board[position.y, position.x] != DefaultChar)
        {
            return false;
        }

        if (decided[position.y, position.x])
        {
            return false;
        }
        decided[position.y, position.x] = true;

        if (Random.Range(0, 100) < 50)
        {
            return false;
        }
        return true;
    }

    void DecideEmptyTilesColor()
    {
        // Todo
    }

    char GetAlphabetFromIndex(int index)
    {
        if (index < 0 || index >= MaxAlphabetCount)
        {
            Debug.LogError($"TileColorGenerator.GetAlphabet() Error :: index {index} 값을 알파벳으로 치환 불가");
            return default;
        }

        return (char)('a' + index);
    }

    char GetAlphabetAt(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= board.GetLength(1)
            || pos.y < 0 || pos.y >= board.GetLength(0))
        {
            Debug.LogError($"TileColorGenerator.GetAlphabetAt() Error :: {pos} 값이 overflow");
            return default;
        }
        return char.ToLower(board[pos.y, pos.x]);
    }

    void DebugLog_Result()
    {
        Debug.Log("----[(Temp)Board]----");

        StringBuilder sb = new StringBuilder();

        for (int y = 0; y < board.GetLength(0); y++)
        {
            for (int x = 0; x < board.GetLength(1); x++)
            {
                char value = board[y, x];
                sb.Append($"{(value == DefaultChar ? '.' : value),3} ");
            }

            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
        Debug.Log("-------------------------");
    }
}
