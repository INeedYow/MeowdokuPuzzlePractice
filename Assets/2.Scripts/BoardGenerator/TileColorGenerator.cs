using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class TileColorGenerator : MonoBehaviour
{
    [Header("Debug Log")]
    [SerializeField] bool showLog = true;
    [Tooltip("타일 색 1차 결정 후 결과 로그")]
    [SerializeField] bool logBoardAfterDecideColorFromCat;

    int totalCatCount;


    // 같은 알파벳은 같은 색을 의미
    // 알파벳 대문자는 해당 색의 고양이 위치를 의미
    // \0 (DefaultChar) : 아직 결정하지 않은 타일
    char[,] board;
    const char DefaultChar = '\0';


    public char[,] Generate(int catCount, List<Vector2Int> catPositions)
    {
        totalCatCount = catCount;
        board = new char[catCount, catCount];

        for (int i = 0; i < catPositions.Count; i++)
        {
            Vector2Int pos = catPositions[i];
            board[pos.y, pos.x] = char.ToUpper(RegionUtility.GetIdFromIndex(i));
        }

        // 1차로 고양이 타일부터 퍼져나가면서 색상 결정
        for (int i = 0; i < catPositions.Count; i++)
        {
            DecideColorFromCat(catPositions[i], RegionUtility.GetIdFromIndex(i));
        }

        if (logBoardAfterDecideColorFromCat)
        {
            Debug.Log("----[색칠 중간 Board 상태 로그]----");
            DebugLog_Result();
        }

        // 2차로 남은 타일들 색상 결정
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

        foreach(var dir in DirectionUtility.Directions4)
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

                foreach (var dir in DirectionUtility.Directions4)
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

    // 아이디어
    // 1. 1차로 DecideColorFromCat에서 색이 배치된 후
    // board를 전체 순회하면서 4방향 인접한 타일 중 색이 결정된 타일이 1개라도 있으면 다음 색 결정 후보로 추가
    // 2. 다음 색 결정 후보에서 무작위 순서로 뽑아 인접한 타일 색들 중 무작위 선택 (처리 순서가 선택 결과에 영향을 줘서 무작위로)
        //  .   .   a       << (1,0) -> (0,0) 순서로 결정하면 (1,0)에는 a,b 가능, (0,0)에는 a,b가능
        //  b   b   .       << (0,0) -> (1,0) 순서로 결정하면 (0,0)에는 b만 가능, (1,0)에는 a,b가능 
    // 3. 색 미결정 타일이 없을 때까지 1,2번 반복
    void DecideEmptyTilesColor()
    {
        while (TryExtractDecidableEmptyTiles(out List<Vector2Int> candidates))
        {
            // 인접한 색이 있는 색 미결정 타일 중에서 무작위 선택
            int randomIndex = Random.Range(0, candidates.Count);
            Vector2Int candidate = candidates[randomIndex];

            // 인접한 색 중에서 무작위 선택
            List<char> neighborColors = new List<char>();
            foreach (var dir in DirectionUtility.Directions4)
            {
                Vector2Int neighborPos = candidate + dir;
                if (TryGetAlphabetAt(neighborPos, out char color))
                {
                    neighborColors.Add(color);
                }
            }

            randomIndex = Random.Range(0, neighborColors.Count);
            board[candidate.y, candidate.x] = neighborColors[randomIndex];
        }
    }

    bool TryExtractDecidableEmptyTiles(out List<Vector2Int> decidableCandidates)
    {
        decidableCandidates = new List<Vector2Int>();

        for (int y = 0; y < board.GetLength(0); y++)
        {
            for (int x = 0; x < board.GetLength(1); x++)
            {
                if (board[y, x] != DefaultChar)
                    continue;

                Vector2Int neighborPos = new Vector2Int(x, y);
                bool hasDecidedNeighborTile = false;
                foreach (var dir in DirectionUtility.Directions4)
                {
                    neighborPos = new Vector2Int(x, y) + dir;
                    if (TryGetAlphabetAt(neighborPos, out char color)
                        && color != DefaultChar)
                    {
                        hasDecidedNeighborTile = true;
                        break;
                    }
                }

                if (hasDecidedNeighborTile)
                    decidableCandidates.Add(new Vector2Int(x, y));
            }
        }

        return decidableCandidates.Count > 0;
    }
    bool IsEmptyTileAt(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= board.GetLength(1)
            || pos.y < 0 || pos.y >= board.GetLength(0))
        {
            //Debug.LogError($"TileColorGenerator.IsEmptyTileAt() Error :: {pos} 값이 overflow");
            return false;
        }

        return board[pos.y, pos.x] == DefaultChar;
    }

    bool TryGetAlphabetAt(Vector2Int pos, out char alphabet)
    {
        alphabet = DefaultChar;
        if (pos.x < 0 || pos.x >= board.GetLength(1)
            || pos.y < 0 || pos.y >= board.GetLength(0))
        {
            //Debug.LogError($"TileColorGenerator.GetAlphabetAt() Error :: {pos} 값이 overflow");
            return false;
        }
        alphabet = char.ToLower(board[pos.y, pos.x]);
        return true;  
    }

    void DebugLog_Result()
    {
        Debug.Log("----[Board]----");

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
