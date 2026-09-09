using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardSolver : MonoBehaviour
{
    char[,] board;
    int totalCatCount;

    List<Vector2Int>[] candidatesPerRegion;
    List<Vector2Int> foundCatPositions;

    // 아이디어
    // 1. 1칸짜리 영역 확정 : 한 영역의 후보가 1개
    // 2. 1줄에 후보가 1개뿐인 경우 확정 : 한 줄에 후보가 1개
    // 3. 1줄에 1개 영역만 존재 : 해당 줄이 아닌 곳에 존재하는 후보들 제거
    // 4. 1개의 영역이 1줄 안에 존재 : 해당 줄에 다른 영역 후보들 제거
    // 5. n개의 줄에 n개의 영역만 존재 : 해당 줄이 아닌 곳에 존재하는 후보들 제거
    // 6. n개의 영역이 n개의 줄 안에 존재 : 해당 줄에 다른 영역 후보들 제거
    // 7. 모순 위치 찾기 : 특정 타일에 고양이가 있을 때 모순인지 확인
    // 1->7로 진행하며, 변경점(후보 제거 등)이 있으면 1로 돌아가서 반복 진행
    public void Solve(char[,] board, int catCount)
    {
        this.board = board;
        totalCatCount = catCount;

        foundCatPositions = new List<Vector2Int>();

        candidatesPerRegion = new List<Vector2Int>[totalCatCount];
        for (int i = 0; i < candidatesPerRegion.Length; i++)
            candidatesPerRegion[i] = new List<Vector2Int>();

        // board 순회하며 candidates에 같은 영역 좌표들끼리 저장
        for (int y = 0; y < board.GetLength(0); y++)
        {
            for (int x = 0; x < board.GetLength(1); x++)
            {
                if (RegionUtility.TryGetIndexFromId(board[y, x], out int index))
                {
                    candidatesPerRegion[index].Add(new Vector2Int(x, y));
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

    #region Solving Rules

    // 1. 1칸짜리 영역 확정 : 한 영역의 후보가 1개
    bool TryFindSingleTileRegion()
    {
        bool hasFound = false;

        for (int i = 0; i < candidatesPerRegion.Length; i++)
        {
            if (candidatesPerRegion[i].Count == 1)
            {
                Vector2Int catPos = candidatesPerRegion[i][0];
                ConfirmCatPosition(catPos);
                hasFound = true;
            }
        }
        return hasFound;
    }
    // 2. 1줄에 후보가 1개뿐인 경우 확정 : 한 줄에 후보가 1개
    bool TryFindSingleCandidateInLine()
    {
        bool hasFound = false;

        List<Vector2Int>[] candidatesInRow = new List<Vector2Int>[totalCatCount];
        List<Vector2Int>[] candidatesInColumn = new List<Vector2Int>[totalCatCount];

        for (int i = 0; i < candidatesInRow.Length; i++)
            candidatesInRow[i] = new List<Vector2Int>();
        for (int i = 0; i < candidatesInColumn.Length; i++)
            candidatesInColumn[i] = new List<Vector2Int>();

        // candidates 정보로 candidatesInRow, candidatesInColumn 생성
        for (int region = 0; region < candidatesPerRegion.Length; region++)
        {
            for (int i = 0; i < candidatesPerRegion[region].Count; i++)
            {
                Vector2Int pos = candidatesPerRegion[region][i];

                candidatesInRow[pos.y].Add(pos);
                candidatesInColumn[pos.x].Add(pos);
            }
        }
        
        // 각 row, column에 후보가 1개라면 확정
        foreach (var candidateList in candidatesInRow)
        {
            if (candidateList.Count == 1)
            {
                ConfirmCatPosition(candidateList[0]);
                hasFound = true;
            }
        }
        foreach (var candidateList in candidatesInColumn)
        {
            if (candidateList.Count == 1)
            {
                ConfirmCatPosition(candidateList[0]);
                hasFound = true;
            }
        }

        return hasFound;
    }
    // 3. 1줄에 1개 영역만 존재 : 해당 줄이 아닌 곳에 존재하는 후보들 제거
    bool TryFindSingleRegionInLine()
    {
        bool hasChanged = false;

        HashSet<char>[] regionsInRow = new HashSet<char>[totalCatCount];
        HashSet<char>[] regionsInColumn = new HashSet<char>[totalCatCount];

        for (int i = 0; i < regionsInRow.Length; i++)
            regionsInRow[i] = new HashSet<char>();
        for (int i = 0; i < regionsInColumn.Length; i++)
            regionsInColumn[i] = new HashSet<char>();

        // candidates 정보로 regionsInRow, regionsInColumn 생성
        for (int region = 0; region < candidatesPerRegion.Length; region++)
        {
            for (int i = 0; i < candidatesPerRegion[region].Count; i++)
            {
                Vector2Int pos = candidatesPerRegion[region][i];
                char regionId = RegionUtility.GetRegionId(board, pos);

                regionsInRow[pos.y].Add(regionId);
                regionsInColumn[pos.x].Add(regionId);
            }
        }

        // 각 row, column에 한 종류의 영역만 존재하면 다른 row, column에 해당 영역 후보들 제거
        for (int row = 0; row < regionsInRow.Length; row++)
        {
            if (regionsInRow[row].Count == 1)
            {
                char regionId = regionsInRow[row].First();

                hasChanged |= RemoveRegionExceptSingleRow(row, regionId);
            }
        }
        for (int col = 0; col < regionsInColumn.Length; col++)
        {
            if (regionsInColumn[col].Count == 1)
            {
                char regionId = regionsInColumn[col].First();

                hasChanged |= RemoveRegionExceptSingleColumn(col, regionId);
            }
        }

        return hasChanged;
    }
    // 4. 1개의 영역이 1줄 안에 존재 : 해당 줄에 다른 영역 후보들 제거 
    bool TryFindSingleLineRegion()
    {
        bool hasChanged = false;
        for (int region = 0; region < candidatesPerRegion.Length; region++)
        {
            if (candidatesPerRegion[region].Count < 2)
                continue;

            bool isSingleRow = true;
            bool isSingleColumn = true;

            int rowIndex = 0;
            int columnIndex = 0;

            List<Vector2Int> candidates = candidatesPerRegion[region];

            for (int i = 0; i < candidates.Count - 1; i++)
            {
                if (isSingleRow)
                {
                    isSingleRow = candidates[i].y == candidates[i + 1].y;

                    if (isSingleRow)
                        rowIndex = candidates[i].y;
                }
                if (isSingleColumn)
                {
                    isSingleColumn = candidates[i].x == candidates[i + 1].x;

                    if (isSingleColumn)
                        columnIndex = candidates[i].x;
                }

                if (!isSingleRow && !isSingleColumn)
                    break;
            }

            if (isSingleRow)
            {
                if (RemoveCandidatesInRow(rowIndex, RegionUtility.GetIdFromIndex(region)))
                    hasChanged = true;
            }
            else if (isSingleColumn)
            {
                if (RemoveCandidatesInColumn(columnIndex, RegionUtility.GetIdFromIndex(region)))
                    hasChanged = true;
            }
        }
        return hasChanged;
    }
    // 5. n개의 줄에 n개의 영역만 존재 : 해당 줄이 아닌 곳에 존재하는 후보들 제거
    bool TryFindMultiRegionInLines()
    {
        // todo
        return false;
    }
    // 6. n개의 영역이 n개의 줄 안에 존재 : 해당 줄에 다른 영역 후보들 제거
    bool TryFindMultiLineRegion()
    {
        bool hasChanged = false;
        // todo
        //HashSet<int>[] rowIndexes = new HashSet<int>[totalCatCount];
        //HashSet<int>[] columnIndexes = new HashSet<int>[totalCatCount];

        //for (int i = 0; i < rowIndexes.Length; i++)
        //    rowIndexes[i] = new HashSet<int>();
        //for (int i = 0; i < columnIndexes.Length; i++)
        //    columnIndexes[i] = new HashSet<int>();

        //for (int i = 0; i < candidatesPerRegion.Length; i++)
        //{
        //    for (int j = 0; j < candidatesPerRegion[i].Count; j++)
        //    {
        //        Vector2Int pos = candidatesPerRegion[i][j];
        //        rowIndexes[i].Add(pos.y);
        //        columnIndexes[i].Add(pos.x);
        //    }
        //}

        //for (int i = 0; i < rowIndexes.Length; i++)
        //{
        //    if (rowIndexes[i].Count <= 1) continue;

        //    
        //}

        return hasChanged;
    }
    // 7. 모순 위치 찾기 : 특정 타일에 고양이가 있을 때 모순인지 확인
    void SolveByAssumption()
    {
        // todo
    }

    #endregion

    #region Cat Position

    void ConfirmCatPosition(Vector2Int catPos)
    {
        if (foundCatPositions.Contains(catPos))
            return;

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

    #endregion

    #region Candidate Management

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

        return candidatesPerRegion[index].Remove(position);
    }
    bool RemoveCandidate(int regionIndex, int candidateIndex)
    {
        if (regionIndex < 0 || regionIndex >= candidatesPerRegion.Length)
            return false;

        if (candidateIndex < 0 || candidateIndex >= candidatesPerRegion[regionIndex].Count)
            return false;

        candidatesPerRegion[regionIndex].RemoveAt(candidateIndex);
        return true;
    }
    bool RemoveCandidatesInRow(int rowIndex, params char[] exceptRegionIds)
    {
        bool hasRemoved = false;
        for (int x = 0; x < board.GetLength(1); x++)
        {
            Vector2Int pos = new Vector2Int(x, rowIndex);
            char regionId = RegionUtility.GetRegionId(board, pos);

            if (exceptRegionIds != null
                && exceptRegionIds.Contains(regionId))
                continue;

            hasRemoved |= RemoveCandidate(pos);
        }
        return hasRemoved;
    }
    bool RemoveRegionExceptSingleRow(int rowIndex, char regionId)
    {
        if (!RegionUtility.TryGetIndexFromId(regionId, out int regionIndex))
            return false;

        bool hasRemoved = false;

        for (int i = candidatesPerRegion[regionIndex].Count - 1; i >= 0; i--)
        {
            if (candidatesPerRegion[regionIndex][i].y != rowIndex)
            {
                hasRemoved |= RemoveCandidate(regionIndex, i);
            }
        }
        return hasRemoved;
    }
    bool RemoveRegionExceptSingleColumn(int columnIndex, char regionId)
    {
        if (!RegionUtility.TryGetIndexFromId(regionId, out int regionIndex))
            return false;

        bool hasRemoved = false;

        for (int i = candidatesPerRegion[regionIndex].Count - 1; i >= 0; i--)
        {
            if (candidatesPerRegion[regionIndex][i].x != columnIndex)
            {
                hasRemoved |= RemoveCandidate(regionIndex, i);
            }
        }
        return hasRemoved;
    }
    bool RemoveCandidatesInColumn(int columnIndex, params char[] exceptRegionIds)
    {
        bool hasRemoved = false;
        for (int y = 0; y < board.GetLength(0); y++)
        {
            Vector2Int pos = new Vector2Int(columnIndex, y);
            char regionId = RegionUtility.GetRegionId(board, pos);  

            if (exceptRegionIds != null
                && exceptRegionIds.Contains(regionId))
                continue;

            hasRemoved |= RemoveCandidate(pos);
        }
        return hasRemoved;
    }
    bool RemoveCandidatesByRegion(Vector2Int catPos)
    {
        char regionId = RegionUtility.GetRegionId(board, catPos);
        if (RegionUtility.TryGetIndexFromId(regionId, out int index))
        {
            if (candidatesPerRegion[index].Count > 0)
            {
                candidatesPerRegion[index].Clear();
                return true;
            }
        }
        return false;
    }

    #endregion
}
