using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardSolver : MonoBehaviour
{
    char[,] board;
    int totalCatCount;

    List<Vector2Int>[] candidates;
    List<Vector2Int> foundCatPositions;

    // 아이디어
    // 1. 1칸짜리 영역 확정 : 한 영역의 후보가 1개
    // 2. 1줄에 후보가 1개뿐인 경우 확정 : 한 줄에 후보가 1개
    // 3. 1줄에 1개 영역만 존재 : 해당 줄이 아닌 곳에 존재하는 후보들 제거
    // 4. 1개의 영역이 1줄 안에 존재 : 해당 줄에 다른 영역 후보들 제거
    // 5. n개의 줄에 n개의 영역만 존재 : 해당 줄이 아닌 곳에 존재하는 후보들 제거
    // 6. n개의 영역이 n개의 줄 안에 존재 : 해당 줄에 다른 영역 후보들 제거
    // 7. 모순 위치 찾기 : 특정 타일에 고양이가 있을 때 모순인지 확인
    // 1->7로 진행하며, 변경점(candidates 제거 등)이 있으면 1로 돌아가서 반복 진행
    public void Solve(char[,] board, int catCount)
    {
        this.board = board;
        totalCatCount = catCount;

        foundCatPositions = new List<Vector2Int>();

        candidates = new List<Vector2Int>[totalCatCount];
        for (int i = 0; i < candidates.Length; i++)
            candidates[i] = new List<Vector2Int>();

        // board 순회하며 candidates에 같은 영역 좌표들끼리 저장
        for (int y = 0; y < board.GetLength(0); y++)
        {
            for (int x = 0; x < board.GetLength(1); x++)
            {
                if (RegionUtility.TryGetIndexFromId(board[y, x], out int index))
                {
                    candidates[index].Add(new Vector2Int(x, y));
                }
            }
        }

        bool shouldExit = false;
        do 
        { 
            // 1. 1칸짜리 영역 확정
            if (TryFindSingleTileRegion()) continue;

            // 2. 1줄에 후보가 1개뿐인 경우 확정
            if (TryFindSingleCandidateInLine()) continue;

            // 3. 1줄에 1개 영역만 존재
            if (TryFindSingleRegionInLine()) continue;

            // 4. 1개의 영역이 1줄 안에 존재
            if (TryFindSingleLineRegion()) continue;

            // 5. n개의 줄에 n개의 영역만 존재
            if (TryFindMultiRegionInLines()) continue;

            // 6. n개의 영역이 n개의 줄 안에 존재
            if (TryFindMultiLineRegion()) continue;

            // 7. 모순 위치 찾기
            SolveByAssumption();

            shouldExit = (foundCatPositions.Count == catCount);
        } while (!shouldExit);
    }

    // 1.
    bool TryFindSingleTileRegion()
    {
        bool hasFound = false;

        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i].Count == 1)
            {
                Vector2Int catPos = candidates[i][0];
                ConfirmCatPosition(catPos);
                hasFound = true;
            }
        }
        return hasFound;
    }

    // 2.
    bool TryFindSingleCandidateInLine()
    {
        // todo
        return false;
    }

    void ConfirmCatPosition(Vector2Int catPos)
    {
        foundCatPositions.Add(catPos);

        // 같은 Row 후보들 제외
        RemoveCandidatesInRow(catPos.y);
        // 같은 Column 후보들 제외
        RemoveCandidatesInColumn(catPos.x);
        // 대각 4방향 후보들 제외
        foreach (var diagonalDir in DirectionUtility.DiagonalDirections)
            RemoveCandidate(catPos + diagonalDir);
        // 같은 색 후보들 제외
        RemoveCandidatesByRegion(catPos);
    }
    // 3.
    bool TryFindSingleRegionInLine()
    {
        bool hasChanged = false;
        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i].Count < 2)
                continue;

            bool isSingleRow = true;
            bool isSingleColumn = true;

            int rowIndex = 0;
            int columnIndex = 0;

            for (int j = 0; j < candidates[i].Count - 1; j++)
            {
                if (isSingleRow)
                {
                    isSingleRow = candidates[i][j].y == candidates[i][j + 1].y;

                    if (isSingleRow) 
                        rowIndex = candidates[i][j].y;
                }
                if (isSingleColumn)
                {
                    isSingleColumn = candidates[i][j].x == candidates[i][j + 1].x;

                    if (isSingleColumn) 
                        columnIndex = candidates[i][j].x;
                }

                if (!isSingleRow && !isSingleColumn)
                    break;
            }

            if (isSingleRow)
            {
                if (RemoveCandidatesInRow(rowIndex, RegionUtility.GetIdFromIndex(i)))
                    hasChanged = true;
            }
            else if (isSingleColumn)
            {
                if (RemoveCandidatesInColumn(columnIndex, RegionUtility.GetIdFromIndex(i)))
                    hasChanged = true;
            }
        }
        return hasChanged;
    }
    // 4.
    bool TryFindSingleLineRegion()
    {
        // todo
        return false;
    }

    // 5.
    bool TryFindMultiRegionInLines()
    {
        // todo
        return false;
    }
    // 6.
    bool TryFindMultiLineRegion()
    {
        bool hasChanged = false;

        HashSet<int>[] rowIndexes = new HashSet<int>[totalCatCount];
        HashSet<int>[] columnIndexes = new HashSet<int>[totalCatCount];

        for (int i = 0; i < rowIndexes.Length; i++)
            rowIndexes[i] = new HashSet<int>();
        for (int i = 0; i < columnIndexes.Length; i++)
            columnIndexes[i] = new HashSet<int>();

        for (int i = 0; i < candidates.Length; i++)
        {
            for (int j = 0; j < candidates[i].Count; j++)
            {
                Vector2Int pos = candidates[i][j];
                rowIndexes[i].Add(pos.y);
                columnIndexes[i].Add(pos.x);
            }
        }

        for (int i = 0; i < rowIndexes.Length; i++)
        {
            if (rowIndexes[i].Count <= 1) continue;

            // todo
        }
        
        return hasChanged;
    }
    // 7.
    void SolveByAssumption()
    {
        // todo
    }


    bool RemoveCandidate(Vector2Int position)
    {
        if (position.x < 0 || position.x >= board.GetLength(1)
            || position.y < 0 || position.y >= board.GetLength(0))
        {
            return false;
        }

        if (!RegionUtility.TryGetIndexFromId(board[position.y, position.x], out int index))
        {
            return false;
        }

        return candidates[index].Remove(position);
    }

    bool RemoveCandidatesInRow(int rowIndex, params char[] exceptRegionIds)
    {
        bool hasRemoved = false;
        for (int x = 0; x < board.GetLength(1); x++)
        {
            Vector2Int pos = new Vector2Int(x, rowIndex);
            char regionId = char.ToLower(board[pos.y, pos.x]);

            if (exceptRegionIds != null
                && exceptRegionIds.Contains(regionId))
                continue;

            hasRemoved |= RemoveCandidate(pos);
        }
        return hasRemoved;
    }

    bool RemoveCandidatesInColumn(int columnIndex, params char[] exceptRegionIds)
    {
        bool hasRemoved = false;
        for (int y = 0; y < board.GetLength(0); y++)
        {
            Vector2Int pos = new Vector2Int(columnIndex, y);
            char regionId = char.ToLower(board[pos.y, pos.x]);

            if (exceptRegionIds != null
                && exceptRegionIds.Contains(regionId))
                continue;

            hasRemoved |= RemoveCandidate(pos);
        }
        return hasRemoved;
    }

    bool RemoveCandidatesByRegion(Vector2Int catPos)
    {
        char regionId = char.ToLower(board[catPos.y, catPos.x]);
        if (RegionUtility.TryGetIndexFromId(regionId, out int index))
        {
            if (candidates[index].Count > 0)
            {
                candidates[index].Clear();
                return true;
            }
        }
        return false;
    }
}
