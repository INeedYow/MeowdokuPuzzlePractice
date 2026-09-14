using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardSolver : MonoBehaviour
{
    [Header("Debug Log")]
    [SerializeField] bool logOnFindSingleTileRegion = true;
    [SerializeField] bool logOnFindSingleCandidateInLine = true;
    [SerializeField] bool logOnFindSingleRegionInLine = true;
    [SerializeField] bool logOnFindSingleLineRegion = true;
    
    char[,] board;
    int totalCatCount;

    List<Vector2Int>[] candidatesPerRegion;
    List<Vector2Int> foundCatPositions;

    // 아이디어
    // 1. 1칸짜리 영역 확정 : 한 영역의 후보가 1개
    // 2. 1줄에 후보가 1개뿐인 경우 확정 : 한 줄에 후보가 1개
    // 3. 1줄에 1개 영역만 존재 : 해당 줄이 아닌 곳에 존재하는 후보들 제거
    // 4. 1개의 영역이 1줄에 모두 존재 : 해당 줄에 다른 영역 후보들 제거
    // 5. n개의 줄에 n개의 영역만 존재 : 해당 줄이 아닌 곳에 존재하는 후보들 제거
    // 6. n개의 영역이 n개의 줄 안에 모두 존재 : 해당 줄에 다른 영역 후보들 제거
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
        int repeatCount = 0;
        do 
        {
            repeatCount++;
            // 1. 1칸짜리 영역 확정
            if (TryFindSingleTileRegion()) continue;

            // 2. 1줄에 후보가 1개뿐인 경우 확정
            if (TryFindSingleCandidateInLine()) continue;

            // 3. 1줄에 1개 영역만 존재
            if (TryFindSingleRegionInLine()) continue;

            // 4. 1개의 영역이 1줄에 모두 존재
            if (TryFindSingleLineInRegion()) continue;

            // 5. n개의 줄에 n개의 영역만 존재
            if (TryFindMultiRegionInLines()) continue;

            // 6. n개의 영역이 n개의 줄 안에 모두 존재
            if (TryFindMultiLineInRegions()) continue;

            // 7. 모순 위치 찾기
            SolveByAssumption();

            //shouldExit = (foundCatPositions.Count == catCount);
            if (foundCatPositions.Count == catCount)
            {
                Debug.Log($"Solver :: 고양이 모두 찾아서 종료");
                foreach (var pos in foundCatPositions)
                    Debug.Log($"{pos}");
                shouldExit = true;
            }
            if (repeatCount > 1000)
            {
                Debug.Log($"Solver :: 반복 횟수 초과로 강제 종료");
                shouldExit = true;
            }
        } while (!shouldExit);
    }

    #region Solving Rules

    // 1. 1칸짜리 영역 확정 : 한 영역의 후보가 1개
    bool TryFindSingleTileRegion()
    {
        bool hasFound = false;

        for (int region = 0; region < candidatesPerRegion.Length; region++)
        {
            if (candidatesPerRegion[region].Count == 1)
            {
                Vector2Int catPos = candidatesPerRegion[region][0];
                ConfirmCatPosition(catPos);
                hasFound = true;

                if (logOnFindSingleTileRegion)
                    Debug.Log($"1. {catPos}에 {RegionUtility.GetIdFromIndex(region)} 1칸 영역 찾음");
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
        foreach (var candidates in candidatesInRow)
        {
            if (candidates.Count == 1)
            {
                ConfirmCatPosition(candidates[0]);
                hasFound = true;

                if (logOnFindSingleCandidateInLine)
                    Debug.Log($"2. Row {candidates[0].y}에 후보가 {candidates[0]} '{RegionUtility.GetRegionId(board, candidates[0])}' 하나만 존재함");
            }
        }
        foreach (var candidates in candidatesInColumn)
        {
            if (candidates.Count == 1)
            {
                ConfirmCatPosition(candidates[0]);
                hasFound = true;

                if (logOnFindSingleCandidateInLine)
                    Debug.Log($"2. Column {candidates[0].x}에 후보가 {candidates[0]} '{RegionUtility.GetRegionId(board, candidates[0])}' 하나만 존재함");
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

                if (RemoveRegionExceptSingleRow(row, regionId))
                {
                    hasChanged = true;

                    if (logOnFindSingleRegionInLine)
                        Debug.Log($"3. Row {row}에 영역이 '{regionId}' 한 종류만 존재함");
                }
            }
        }
        for (int col = 0; col < regionsInColumn.Length; col++)
        {
            if (regionsInColumn[col].Count == 1)
            {
                char regionId = regionsInColumn[col].First();

                if (RemoveRegionExceptSingleColumn(col, regionId))
                {
                    hasChanged = true;

                    if (logOnFindSingleRegionInLine)
                        Debug.Log($"3. Column {col}에 영역이 '{regionId}' 한 종류만 존재함");
                }
            }
        }

        return hasChanged;
    }
    // 4. 1개의 영역이 1줄에 모두 존재 : 해당 줄에 다른 영역 후보들 제거
    bool TryFindSingleLineInRegion()
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
                char regionId = RegionUtility.GetIdFromIndex(region);
                if (RemoveCandidatesInRow(rowIndex, regionId))
                {
                    hasChanged = true;

                    if (logOnFindSingleLineRegion)
                        Debug.Log($"4. '{regionId}' 영역이 Row {rowIndex}에 모두 존재함");
                }
            }
            else if (isSingleColumn)
            {
                char regionId = RegionUtility.GetIdFromIndex(region);
                if (RemoveCandidatesInColumn(columnIndex, regionId))
                {
                    hasChanged = true;

                    if (logOnFindSingleLineRegion)
                        Debug.Log($"4. '{regionId}' 영역이 Column {columnIndex}에 모두 존재함");
                }
            }
        }
        return hasChanged;
    }
    // 5. n개의 줄에 n개의 영역만 존재 : 해당 줄이 아닌 곳에 존재하는 후보들 제거
    bool TryFindMultiRegionInLines()
    {
        List<int>[] regionsInRow = new List<int>[totalCatCount];
        List<int>[] regionsInColumn = new List<int>[totalCatCount];

        for (int i = 0; i < regionsInRow.Length; i++)
            regionsInRow[i] = new List<int>();
        for (int i = 0; i < regionsInColumn.Length; i++)
            regionsInColumn[i] = new List<int>();

        // candidates 정보로 regionsInRow, regionsInColumn 생성
        for (int region = 0; region < candidatesPerRegion.Length; region++)
        {
            for (int i = 0; i < candidatesPerRegion[region].Count; i++)
            {
                Vector2Int pos = candidatesPerRegion[region][i];

                if (!regionsInRow[pos.y].Contains(region))
                    regionsInRow[pos.y].Add(region);
                if (!regionsInColumn[pos.x].Contains(region))
                    regionsInColumn[pos.x].Add(region);
            }
        }

        // 5.6. 함수가 서로 보완 관계여서 maxLineCount는 totalCatCount의 절반까지만
        // ex. 10x10 보드에서 9개의 영역이 9개의 줄 안에 모두 존재 -> 남은 1줄에 1개의 영역만 존재
        int maxLineCount = totalCatCount / 2;
        for (int n = 2; n < maxLineCount; n++)
        {
            List<int> selectedRows = new List<int>();
            if (TryFindRegionCombinationInRows(n, 0, selectedRows, regionsInRow))
            {
                return true;
            }
        }
        for (int n = 2; n < maxLineCount; n++)
        {
            List<int> selectedCols = new List<int>();
            if (TryFindRegionCombinationInColumns(n, 0, selectedCols, regionsInColumn))
            {
                return true;
            }
        }

        return false;
    }
    bool TryFindRegionCombinationInRows(int targetCount, int startIndex, List<int> selectedRows, List<int>[] regionsInRow)
    {
        // 영역 targetCount개 선택완료 -> 검사
        if (selectedRows.Count == targetCount)
        {
            // 선택한 줄들에 위치한 후보 영역들의 합집합
            HashSet<int> regions = new HashSet<int>();

            for (int i = 0; i < selectedRows.Count; i++)
            {
                int row = selectedRows[i];

                foreach (var region in regionsInRow[row])
                    regions.Add(region);
            }

            // 선택한 줄들에 targetCount개의 영역만 존재
            if (regions.Count == targetCount)
            {
                char[] selectedRegionIds = new char[regions.Count];
                int i = 0;
                foreach (var region in regions)
                    selectedRegionIds[i++] = RegionUtility.GetIdFromIndex(region);

                // 선택한 줄들이 아닌 곳에 위치한 해당 영역 후보들 제거
                bool hasRemoved = false;
                for (int row = 0; row < totalCatCount; row++)
                {
                    if (selectedRows.Contains(row))
                        continue;

                    hasRemoved |= RemoveCandidatesInRow(row, selectedRegionIds);
                }
                // 후보 제거에 성공했으면 true 반환하면서 재귀 종료
                return hasRemoved;
            }
            else
            {
                return false;
            }
        }
        // 줄 선택 재귀, selectedRows 중복 선택을 막기 위해 startIndex로 조절
        for (int region = startIndex; region < regionsInRow.Length; region++)
        {
            selectedRows.Add(region);

            // 후보 제거에 성공
            if (TryFindRegionCombinationInRows(targetCount, region + 1, selectedRows, regionsInRow))
            {
                return true;
            }

            // 후보 제거에 실패했으면 다른 영역 선택
            selectedRows.RemoveAt(selectedRows.Count - 1);
        }

        return false;
    }
    bool TryFindRegionCombinationInColumns(int targetCount, int startIndex, List<int> selectedCols, List<int>[] regionsInCol)
    {
        // 영역 targetCount개 선택완료 -> 검사
        if (selectedCols.Count == targetCount)
        {
            // 선택한 줄들에 위치한 후보 영역들의 합집합
            HashSet<int> regions = new HashSet<int>();

            for (int i = 0; i < selectedCols.Count; i++)
            {
                int col = selectedCols[i];

                foreach (var region in regionsInCol[col])
                    regions.Add(region);
            }

            // 선택한 줄들에 targetCount개의 영역만 존재
            if (regions.Count == targetCount)
            {
                char[] selectedRegionIds = new char[regions.Count];
                int i = 0;
                foreach (var region in regions)
                    selectedRegionIds[i++] = RegionUtility.GetIdFromIndex(region);

                // 선택한 줄들이 아닌 곳에 위치한 해당 영역 후보들 제거
                bool hasRemoved = false;
                for (int col = 0; col < totalCatCount; col++)
                {
                    if (selectedCols.Contains(col))
                        continue;

                    hasRemoved |= RemoveCandidatesInColumn(col, selectedRegionIds);
                }
                // 후보 제거에 성공했으면 true 반환하면서 재귀 종료
                return hasRemoved;
            }
            else
            {
                return false;
            }
        }
        // 줄 선택 재귀, selectedCols 중복 선택을 막기 위해 startIndex로 조절
        for (int region = startIndex; region < regionsInCol.Length; region++)
        {
            selectedCols.Add(region);

            // 후보 제거에 성공
            if (TryFindRegionCombinationInColumns(targetCount, region + 1, selectedCols, regionsInCol))
            {
                return true;
            }

            // 후보 제거에 실패했으면 다른 영역 선택
            selectedCols.RemoveAt(selectedCols.Count - 1);
        }

        return false;
    }

    // 6. n개의 영역이 n개의 줄 안에 모두 존재 : 해당 줄에 다른 영역 후보들 제거
    bool TryFindMultiLineInRegions()
    {
        List<int>[] rowIndexesPerRegion = new List<int>[totalCatCount];
        List<int>[] colIndexesPerRegion = new List<int>[totalCatCount];

        for (int i = 0; i < rowIndexesPerRegion.Length; i++)
            rowIndexesPerRegion[i] = new List<int>();
        for (int i = 0; i < colIndexesPerRegion.Length; i++)
            colIndexesPerRegion[i] = new List<int>();

        // candidates 정보로 rowIndexesPerRegion, colIndexesPerRegion 생성
        for (int region = 0; region < candidatesPerRegion.Length; region++)
        {
            for (int i = 0; i < candidatesPerRegion[region].Count; i++)
            {
                Vector2Int pos = candidatesPerRegion[region][i];

                if (!rowIndexesPerRegion[region].Contains(pos.y))
                    rowIndexesPerRegion[region].Add(pos.y);
                if (!colIndexesPerRegion[region].Contains(pos.x))
                    colIndexesPerRegion[region].Add(pos.x);
            }
        }

        // 5.6. 함수가 서로 보완 관계여서 maxLineCount는 totalCatCount의 절반까지만
        // ex. 10x10 보드에서 9개의 영역이 9개의 줄 안에 모두 존재 -> 남은 1줄에 1개의 영역만 존재
        int maxLineCount = totalCatCount / 2;
        for (int n = 2; n < maxLineCount; n++)
        {
            List<int> selectedRegions = new List<int>();
            if (TryFindRowCombinationInRegions(n, 0, selectedRegions, rowIndexesPerRegion))
            {
                return true;
            }
        }
        for (int n = 2; n < maxLineCount; n++)
        {
            List<int> selectedRegions = new List<int>();
            if (TryFindColumnCombinationInRegions(n, 0, selectedRegions, colIndexesPerRegion))
            {
                return true;
            }
        }

        return false;
    }
    bool TryFindRowCombinationInRegions(int targetCount, int startIndex, List<int> selectedRegions, List<int>[] rowIndexesPerRegion)
    {
        // 영역 targetCount개 선택완료 -> 검사
        if (selectedRegions.Count == targetCount)
        {
            // 선택한 영역들이 위치한 rowIndexes의 합집합
            HashSet<int> indexes = new HashSet<int>();

            for (int i = 0; i < selectedRegions.Count; i++)
            {
                int region = selectedRegions[i];

                foreach (var rowIndex in rowIndexesPerRegion[region])
                    indexes.Add(rowIndex);
            }

            // 선택한 영역들이 targetCount개의 줄 안에 모두 위치함
            if (indexes.Count == targetCount)
            {
                char[] selectedRegionIds = new char[selectedRegions.Count];
                for (int i = 0; i < selectedRegions.Count; i++)
                    selectedRegionIds[i] = RegionUtility.GetIdFromIndex(selectedRegions[i]);

                // 해당 줄들에 다른 영역 후보들 제거
                bool hasRemoved = false;
                foreach (var index in indexes)
                {
                    hasRemoved |= RemoveCandidatesInRow(index, selectedRegionIds);
                }
                // 후보 제거에 성공했으면 true 반환하면서 재귀 종료
                return hasRemoved;
            }
            else
            {
                return false;
            }
        }
        // 영역 선택 재귀, selectedRegions 중복 선택을 막기 위해 startIndex로 조절
        for (int region = startIndex; region < rowIndexesPerRegion.Length; region++)
        {
            selectedRegions.Add(region);

            // 후보 제거에 성공
            if (TryFindRowCombinationInRegions(targetCount, region + 1, selectedRegions, rowIndexesPerRegion))
            {
                return true;
            }

            // 후보 제거에 실패했으면 다른 영역 선택
            selectedRegions.RemoveAt(selectedRegions.Count - 1);
        }

        return false;
    }
    bool TryFindColumnCombinationInRegions(int targetCount, int startIndex, List<int> selectedRegions, List<int>[] colIndexesPerRegion)
    {
        // 영역 targetCount개 선택완료 -> 검사
        if (selectedRegions.Count == targetCount)
        {
            // 선택한 영역들이 위치한 colIndexes의 합집합
            HashSet<int> indexes = new HashSet<int>();

            for (int i = 0; i < selectedRegions.Count; i++)
            {
                int region = selectedRegions[i];

                foreach (var colIndex in colIndexesPerRegion[region])
                    indexes.Add(colIndex);
            }

            // 선택한 영역들이 targetCount개의 줄 안에 모두 위치함
            if (indexes.Count == targetCount)
            {
                char[] selectedRegionIds = new char[selectedRegions.Count];
                for (int i = 0; i < selectedRegions.Count; i++)
                    selectedRegionIds[i] = RegionUtility.GetIdFromIndex(selectedRegions[i]);

                // 해당 줄들에 다른 영역 후보들 제거
                bool hasRemoved = false;
                foreach (var index in indexes)
                {
                    hasRemoved |= RemoveCandidatesInColumn(index, selectedRegionIds);
                }
                // 후보 제거에 성공했으면 true 반환하면서 재귀 종료
                return hasRemoved;
            }
            else
            {
                return false;
            }
        }
        // 영역 선택 재귀, selectedRegions 중복 선택을 막기 위해 startIndex로 조절
        for (int region = startIndex; region < colIndexesPerRegion.Length; region++)
        {
            selectedRegions.Add(region);

            // 후보 제거에 성공
            if (TryFindColumnCombinationInRegions(targetCount, region + 1, selectedRegions, colIndexesPerRegion))
            {
                return true;
            }

            // 후보 제거에 실패했으면 다른 영역 선택
            selectedRegions.RemoveAt(selectedRegions.Count - 1);
        }

        return false;
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
        
        // Debug Log용 변수
        //Vector2Int removedPosition = candidatesPerRegion[regionIndex][candidateIndex];

        candidatesPerRegion[regionIndex].RemoveAt(candidateIndex);
        //Debug.Log($"RemoveCandidate : '{RegionUtility.GetIdFromIndex(regionIndex)}' {removedPosition} 삭제");

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
