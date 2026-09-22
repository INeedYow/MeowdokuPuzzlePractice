using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardSolver : MonoBehaviour
{
    [Header("Debug Log")]
    [Tooltip("후보 삭제 로그")]
    [SerializeField] bool logOnRemoveCandidate = false;
    [Tooltip("1번 규칙 로그")]
    [SerializeField] bool logOnFindSingleTileRegion = true;
    [Tooltip("2번 규칙 로그")]
    [SerializeField] bool logOnFindSingleCandidateInLine = true;
    [Tooltip("3번 규칙 로그")]
    [SerializeField] bool logOnFindSingleRegionInLine = true;
    [Tooltip("4번 규칙 로그")]
    [SerializeField] bool logOnFindSingleLineInRegion = true;
    [Tooltip("5번 규칙 로그")]
    [SerializeField] bool logOnFindContradictionRegion = true;
    [Tooltip("6번 규칙 로그")]
    [SerializeField] bool logOnFindMultiRegionInLines = true;
    [Tooltip("7번 규칙 로그")]
    [SerializeField] bool logOnFindMultiLineInRegion = true;
    [Tooltip("8번 규칙 로그")]
    [SerializeField] bool logOnFindContradictionCandidate = true;


    char[,] board;
    int totalCatCount;

    List<Vector2Int>[] candidatesPerRegion;
    List<Vector2Int> foundCatPositions;
    bool[] hasFoundCatPerRegion;


    // 아이디어
    // 1. 1칸짜리 영역 확정 : 한 영역의 후보가 1개
    // 2. 1줄에 후보가 1개뿐인 경우 확정 : 한 줄에 후보가 1개
    // 3. 1줄에 1개 영역만 존재 : 해당 줄이 아닌 곳에 존재하는 후보들 제거
    // 4. 1개의 영역이 1줄에 모두 존재 : 해당 줄에 다른 영역 후보들 제거
    // 5. 다른 영역의 후보를 모두 제거하는 후보 제거
    // 6. n개의 줄에 n개의 영역만 존재 : 해당 줄이 아닌 곳에 존재하는 후보들 제거
    // 7. n개의 영역이 n개의 줄 안에 모두 존재 : 해당 줄에 다른 영역 후보들 제거
    // 8. 모순 위치 찾기 : 특정 후보가 고양이일 때 모순인지 확인
        // 모든 후보를 대상으로 하지 않고, 다른 영역의 후보를 1개로 만드는 후보만 탐색
    // 1->8로 진행하며, 변경점(후보 제거 등)이 있으면 1로 돌아가서 반복 진행
    // 1~8 한 사이클 진행하는 동안 후보의 변경이 없었다면 풀이가 불가능하다고 판단
    public void Solve(char[,] board, int catCount)
    {
        this.board = board;
        totalCatCount = catCount;

        foundCatPositions = new List<Vector2Int>();

        candidatesPerRegion = new List<Vector2Int>[totalCatCount];
        for (int i = 0; i < candidatesPerRegion.Length; i++)
            candidatesPerRegion[i] = new List<Vector2Int>();

        hasFoundCatPerRegion = new bool[totalCatCount];

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

        while (true)
        {
            // 모든 고양이 찾기 성공
            if (foundCatPositions.Count == catCount)
            {
                Debug.Log($"Solver :: 고양이 모두 찾아서 종료 ({catCount} 마리) \n {string.Join(", ", foundCatPositions)}");
                break;
            }

            // 1. 1칸짜리 영역 확정
            if (TryFindSingleCandidateRegion()) continue;

            // 2. 1줄에 후보가 1개뿐인 경우 확정
            if (TryFindSingleCandidateInLine()) continue;

            // 3. 1줄에 1개 영역만 존재
            if (TryFindSingleRegionInLine()) continue;

            // 4. 1개의 영역이 1줄에 모두 존재
            if (TryFindSingleLineInRegion()) continue;

            // 5. 다른 영역의 후보를 모두 제거하는 후보 제거
            if (TryRemoveCandidatesClearingOtherRegions()) continue;

            // 6. n개의 줄에 n개의 영역만 존재
            if (TryFindMultiRegionInLines()) continue;

            // 7. n개의 영역이 n개의 줄 안에 모두 존재
            if (TryFindMultiLineInRegions()) continue;

            // 8. 모순 위치 찾기
            if (!TryRemoveCandidatesByAssumption())
            {
                Debug.Log($"Solver :: 현재 규칙으로 풀이를 더 이상 진행할 수 없음");
                return;
            }
        }
    }

    #region Solving Rules

    // 1. 1칸짜리 영역 확정 : 한 영역의 후보가 1개
    bool TryFindSingleCandidateRegion()
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

                if (RemoveRegionExceptSingleRow(candidatesPerRegion, row, regionId))
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

                if (RemoveRegionExceptSingleColumn(candidatesPerRegion, col, regionId))
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
                if (RemoveCandidatesInRowExcept(candidatesPerRegion, rowIndex, true, regionId))
                {
                    hasChanged = true;

                    if (logOnFindSingleLineInRegion)
                        Debug.Log($"4. '{regionId}' 영역이 Row {rowIndex}에 모두 존재함");
                }
            }
            else if (isSingleColumn)
            {
                char regionId = RegionUtility.GetIdFromIndex(region);
                if (RemoveCandidatesInColumnExcept(candidatesPerRegion, columnIndex, true, regionId))
                {
                    hasChanged = true;

                    if (logOnFindSingleLineInRegion)
                        Debug.Log($"4. '{regionId}' 영역이 Column {columnIndex}에 모두 존재함");
                }
            }
        }
        return hasChanged;
    }
    // 5. 다른 영역의 후보를 모두 제거하는 후보 제거
        // 사람과 유사하게 풀이할 수 있도록, 한 사이클에서는 하나의 모순만 해결함.
        // 처음 발견한 모순 영역을 기준으로 해당 영역의 후보를 모두 제거하는 후보만 제거하고,
        // 다른 영역의 모순은 다음 사이클에서 처리하도록 함.
    bool TryRemoveCandidatesClearingOtherRegions()
    {
        for (int region = 0; region < totalCatCount; region++)
        {
            // 고양이 이미 찾은 영역 제외
            if (HasFoundCat(region)) 
                continue;

            // 처음 모순 발견한 대상 영역 저장용 변수
            int contradictionRegion = -1;

            // candidatesPerRegion 순회 후 일괄 제거를 위해 제거 대상 저장
            List<Vector2Int> removeTargets = new List<Vector2Int>();

            for (int i = 0; i < candidatesPerRegion[region].Count; i++)
            {
                // 현재 candidatesPerRegion의 복사본 생성
                List<Vector2Int>[] tempCandidatesPerRegion = new List<Vector2Int>[totalCatCount];
                for (int j = 0; j < totalCatCount; j++)
                    tempCandidatesPerRegion[j] = new List<Vector2Int>(candidatesPerRegion[j]);

                Vector2Int target = candidatesPerRegion[region][i];

                // 한 후보를 고양이라고 가정
                AssumeCatPosition(tempCandidatesPerRegion, target);

                // 가정 후 tempCandidatesPerRegion 탐색
                for (int tempRegion = 0; tempRegion < tempCandidatesPerRegion.Length; tempRegion++)
                {
                    // 같은 영역이면
                    if (tempRegion == region)
                        continue;

                    // 이미 고양이 찾은 영역이면
                    if (HasFoundCat(tempRegion))
                        continue;

                    // 다른 영역 후보가 모두 제거되지 않았으면
                    if (tempCandidatesPerRegion[tempRegion].Count != 0)
                        continue;

                    // 처음 모순 영역을 발견한 게 아니면서 처음 발견한 모순 영역과 다르면
                    if (contradictionRegion != -1 && tempRegion != contradictionRegion)
                        continue;

                    // 처음 모순 발견한 대상 영역 저장
                    contradictionRegion = tempRegion;
                    // 모순인 후보 제거 대상으로
                    removeTargets.Add(target);
                    //Debug.Log($"    5. region {region} / contradictionRegion {contradictionRegion} / targetPos {target}");
                }
            }

            // candidatesPerRegion 순회 후 일괄 제거
            if (contradictionRegion != -1)
            {
                foreach(var removeTarget in removeTargets)
                    RemoveCandidate(candidatesPerRegion, removeTarget);

                // log
                if (logOnFindContradictionRegion)
                {
                    char regionId = RegionUtility.GetIdFromIndex(region);
                    char contradictionRegionId = RegionUtility.GetIdFromIndex(contradictionRegion);
                    Debug.Log($"5. 영역 '{regionId}'의 {string.Join(", ", removeTargets)} 후보 제거 // 모순 영역 = '{contradictionRegionId}'");
                }
                    
                return true;
            }
        }
        return false;
    }
    void AssumeCatPosition(List<Vector2Int>[] candidatesPerRegion, Vector2Int catPos)
    {
        // 같은 Row 후보들 제외
        RemoveCandidatesInRow(candidatesPerRegion, catPos.y, false);
        // 같은 Column 후보들 제외
        RemoveCandidatesInColumn(candidatesPerRegion, catPos.x, false);
        // 대각 4방향 후보들 제외
        foreach (var diagonalDir in DirectionUtility.DiagonalDirections)
            RemoveCandidate(candidatesPerRegion, catPos + diagonalDir, false);
        // 같은 영역 후보들 제외
        RemoveCandidatesByRegion(candidatesPerRegion, catPos, false);
    }
    // 6. n개의 줄에 n개의 영역만 존재 : 해당 줄이 아닌 곳에 존재하는 후보들 제거
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

        // 함수가 서로 보완 관계여서 maxLineCount는 totalCatCount의 절반까지만
        // ex. 10x10 보드에서 9개의 영역이 9개의 줄 안에 모두 존재 -> 남은 1줄에 1개의 영역만 존재
        int maxLineCount = totalCatCount / 2;
        for (int n = 2; n <= maxLineCount; n++)
        {
            List<int> selectedRows = new List<int>();
            if (TryFindRegionCombinationInRows(n, 0, selectedRows, regionsInRow))
            {
                return true;
            }
        }
        for (int n = 2; n <= maxLineCount; n++)
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

                    hasRemoved |= RemoveCandidatesInRowOnly(
                        candidatesPerRegion, row, true, selectedRegionIds);
                }
                // log
                if (logOnFindMultiRegionInLines && hasRemoved)
                {
                    Debug.Log($"6. Row {string.Join(",", selectedRows)}에 {string.Join(",", selectedRegionIds)} 영역만 존재");
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
        for (int row = startIndex; row < regionsInRow.Length; row++)
        {
            // 후보가 없는 줄은 스킵
            if (regionsInRow[row].Count == 0)
                continue;

            selectedRows.Add(row);

            // 후보 제거에 성공
            if (TryFindRegionCombinationInRows(targetCount, row + 1, selectedRows, regionsInRow))
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

                    hasRemoved |= RemoveCandidatesInColumnOnly(
                        candidatesPerRegion, col, true, selectedRegionIds);
                }
                // log
                if (logOnFindMultiRegionInLines && hasRemoved)
                {
                    Debug.Log($"6. Column {string.Join(",", selectedCols)}에 {string.Join(",", selectedRegionIds)} 영역만 존재");
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
        for (int col = startIndex; col < regionsInCol.Length; col++)
        {
            // 후보가 없는 줄은 스킵
            if (regionsInCol[col].Count == 0)
                continue;

            selectedCols.Add(col);

            // 후보 제거에 성공
            if (TryFindRegionCombinationInColumns(targetCount, col + 1, selectedCols, regionsInCol))
            {
                return true;
            }

            // 후보 제거에 실패했으면 다른 영역 선택
            selectedCols.RemoveAt(selectedCols.Count - 1);
        }

        return false;
    }

    // 7. n개의 영역이 n개의 줄 안에 모두 존재 : 해당 줄에 다른 영역 후보들 제거
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

        // 함수가 서로 보완 관계여서 maxLineCount는 totalCatCount의 절반까지만
        // ex. 10x10 보드에서 9개의 영역이 9개의 줄 안에 모두 존재 -> 남은 1줄에 1개의 영역만 존재
        int maxLineCount = totalCatCount / 2;
        for (int n = 2; n <= maxLineCount; n++)
        {
            List<int> selectedRegions = new List<int>();
            if (TryFindRowCombinationInRegions(n, 0, selectedRegions, rowIndexesPerRegion))
            {
                return true;
            }
        }
        for (int n = 2; n <= maxLineCount; n++)
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
                    hasRemoved |= RemoveCandidatesInRowExcept(  
                        candidatesPerRegion, index, true, selectedRegionIds);
                }
                // log
                if (logOnFindMultiLineInRegion && hasRemoved)
                {
                    Debug.Log($"7. {string.Join(",", selectedRegionIds)} 영역들이 Row {string.Join(",", indexes)} 안에 모두 존재");
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
            // 고양이 이미 찾은 영역이면 스킵
            if (HasFoundCat(region))
                continue;

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
                    hasRemoved |= RemoveCandidatesInColumnExcept(
                        candidatesPerRegion, index, true, selectedRegionIds);
                }
                // log
                if (logOnFindMultiLineInRegion && hasRemoved)
                {
                    Debug.Log($"7. {string.Join(",", selectedRegionIds)} 영역들이 Column {string.Join(",", indexes)} 안에 모두 존재");
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
            // 고양이 이미 찾은 영역이면 스킵
            if (HasFoundCat(region))
                continue;

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

    // 8. 모순 위치 찾기 : 특정 후보가 고양이일 때 모순인지 확인
    bool TryRemoveCandidatesByAssumption()
    {
        for (int region = 0; region < totalCatCount; region++)
        {
            // 고양이 이미 찾은 영역 제외
            if (HasFoundCat(region))
                continue;

            for (int i = 0; i < candidatesPerRegion[region].Count; i++)
            {
                List<Vector2Int>[] tempCandidatesPerRegion = new List<Vector2Int>[totalCatCount];

                for (int j = 0; j < totalCatCount; j++)
                    tempCandidatesPerRegion[j] = new List<Vector2Int>(candidatesPerRegion[j]);

                bool[] tempHasFoundCatPerRegion = new bool[totalCatCount];
                for (int k = 0; k < tempHasFoundCatPerRegion.Length; k++)
                    tempHasFoundCatPerRegion[k] = hasFoundCatPerRegion[k];

                Vector2Int target = candidatesPerRegion[region][i];

                // 한 후보를 고양이라고 가정했을 때 모순 발생 -> 해당 후보 제거
                if (TryFindContradictionByAssumption(tempCandidatesPerRegion, tempHasFoundCatPerRegion, target, region))
                {
                    if (logOnFindContradictionCandidate)
                        Debug.Log($"8. {target} 후보가 모순이라서 제거");
                    RemoveCandidate(candidatesPerRegion, target);
                    return true;
                }
            }
        }
        return false;
    }
    bool TryFindContradictionByAssumption(List<Vector2Int>[] candidatesPerRegion, bool[] hasFoundCatPerRegion, Vector2Int catPos, int region)
    {
        // 한 후보를 고양이라고 가정
        AssumeCatPosition(candidatesPerRegion, catPos);
        hasFoundCatPerRegion[region] = true;

        // 현재 가정으로 모든 고양이 찾기 성공하면 종료
        bool hasFoundAllCat = true;
        foreach (var hasFoundCat in hasFoundCatPerRegion)
            if (!hasFoundCat)
                hasFoundAllCat = false;

        if (hasFoundAllCat)
            return false;
        
        // 가정 후 다른 영역 확인
        for (int tempRegion = 0; tempRegion < candidatesPerRegion.Length; tempRegion++)
        {
            // 같은 영역이면
            if (tempRegion == region)
                continue;

            // 이미 고양이 찾은 영역이면 (기존에 찾았던 영역 + 가정한 영역)
            if (hasFoundCatPerRegion[tempRegion])
                continue;

            // 모순 발견(다른 영역 후보를 모두 제거)
            if (candidatesPerRegion[tempRegion].Count == 0)
                return true;

            // 다른 영역 후보를 1개만 남기는 후보만
            if (candidatesPerRegion[tempRegion].Count != 1)
                continue;

            Vector2Int target = candidatesPerRegion[tempRegion][0];

            return TryFindContradictionByAssumption(candidatesPerRegion, hasFoundCatPerRegion, target, tempRegion);
        }
        // 더 이상 연쇄 확인할 후보가 없음 (후보가 1개인 영역이 없음)
        return false;
    }

    #endregion

    #region Cat Position

    void ConfirmCatPosition(Vector2Int catPos)
    {
        if (foundCatPositions.Contains(catPos))
        {
            Debug.LogError($"BoardSolver.ConfirmCatPosition() Error :: catPos {catPos}에 이미 고양이 확정됨");
            return;
        }

        char regionId = RegionUtility.GetRegionId(board, catPos);
        if (!RegionUtility.TryGetIndexFromId(regionId, out int region))
        {
            Debug.LogError($"BoardSolver.ConfirmCatPosition() Error :: catPos {catPos}의 regionId('{regionId}')를 int로 변환실패");
            return;
        }

        if (hasFoundCatPerRegion[region])
        {
            Debug.LogError($"BoardSolver.ConfirmCatPosition() Error :: 영역 index ({region})의 고양이를 이미 찾음 // catPos = {catPos} // regionId = '{regionId}'");
            return;
        }

        foundCatPositions.Add(catPos);
        hasFoundCatPerRegion[region] = true;

        // 같은 Row 후보들 제외
        RemoveCandidatesInRow(candidatesPerRegion, catPos.y);
        // 같은 Column 후보들 제외
        RemoveCandidatesInColumn(candidatesPerRegion, catPos.x);
        // 대각 4방향 후보들 제외
        foreach (var diagonalDir in DirectionUtility.DiagonalDirections)
            RemoveCandidate(candidatesPerRegion, catPos + diagonalDir);
        // 같은 영역 후보들 제외
        RemoveCandidatesByRegion(candidatesPerRegion, catPos);
    }

    #endregion

    #region Candidate Management

    bool RemoveCandidate(List<Vector2Int>[] candidatesPerRegion, Vector2Int position, bool allowLog = true)
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

        if (allowLog && logOnRemoveCandidate && candidatesPerRegion[index].Contains(position))
            Debug.Log($"RemoveCandidate :: {position} / '{board[position.y, position.x]}'");

        return candidatesPerRegion[index].Remove(position);
    }
    bool RemoveCandidatesInRow(List<Vector2Int>[] candidatesPerRegion, int rowIndex, bool allowLog = true)
    {
        bool hasRemoved = false;
        for (int x = 0; x < board.GetLength(1); x++)
        {
            Vector2Int pos = new Vector2Int(x, rowIndex);
            char regionId = RegionUtility.GetRegionId(board, pos);

            hasRemoved |= RemoveCandidate(candidatesPerRegion, pos, allowLog);
        }
        return hasRemoved;
    }
    bool RemoveCandidatesInRowExcept(List<Vector2Int>[] candidatesPerRegion, int rowIndex, bool allowLog = true, params char[] exceptRegionIds)
    {
        bool hasRemoved = false;
        for (int x = 0; x < board.GetLength(1); x++)
        {
            Vector2Int pos = new Vector2Int(x, rowIndex);
            char regionId = RegionUtility.GetRegionId(board, pos);

            if (exceptRegionIds != null
                && exceptRegionIds.Contains(regionId))
                continue;

            hasRemoved |= RemoveCandidate(candidatesPerRegion, pos, allowLog);
        }
        return hasRemoved;
    }
    bool RemoveCandidatesInRowOnly(List<Vector2Int>[] candidatesPerRegion, int rowIndex, bool allowLog = true, params char[] targetRegionIds)
    {
        bool hasRemoved = false;
        for (int x = 0; x < board.GetLength(1); x++)
        {
            Vector2Int pos = new Vector2Int(x, rowIndex);
            char regionId = RegionUtility.GetRegionId(board, pos);

            if (targetRegionIds == null
                || !targetRegionIds.Contains(regionId))
                continue;

            hasRemoved |= RemoveCandidate(candidatesPerRegion, pos, allowLog);
        }
        return hasRemoved;
    }
    bool RemoveCandidatesInColumn(List<Vector2Int>[] candidatesPerRegion, int columnIndex, bool allowLog = true)
    {
        bool hasRemoved = false;
        for (int y = 0; y < board.GetLength(0); y++)
        {
            Vector2Int pos = new Vector2Int(columnIndex, y);
            char regionId = RegionUtility.GetRegionId(board, pos);  

            hasRemoved |= RemoveCandidate(candidatesPerRegion, pos, allowLog);
        }
        return hasRemoved;
    }
    bool RemoveCandidatesInColumnExcept(List<Vector2Int>[] candidatesPerRegion, int columnIndex, bool allowLog, params char[] exceptRegionIds)
    {
        bool hasRemoved = false;
        for (int y = 0; y < board.GetLength(0); y++)
        {
            Vector2Int pos = new Vector2Int(columnIndex, y);
            char regionId = RegionUtility.GetRegionId(board, pos);

            if (exceptRegionIds != null
                && exceptRegionIds.Contains(regionId))
                continue;

            hasRemoved |= RemoveCandidate(candidatesPerRegion, pos, allowLog);
        }
        return hasRemoved;
    }
    bool RemoveCandidatesInColumnOnly(List<Vector2Int>[] candidatesPerRegion, int columnIndex, bool allowLog, params char[] targetRegionIds)
    {
        bool hasRemoved = false;
        for (int y = 0; y < board.GetLength(0); y++)
        {
            Vector2Int pos = new Vector2Int(columnIndex, y);
            char regionId = RegionUtility.GetRegionId(board, pos);

            if (targetRegionIds == null
                || !targetRegionIds.Contains(regionId))
                continue;

            hasRemoved |= RemoveCandidate(candidatesPerRegion, pos, allowLog);
        }
        return hasRemoved;
    }
    bool RemoveRegionExceptSingleRow(List<Vector2Int>[] candidatesPerRegion, int rowIndex, char regionId, bool allowLog = true)
    {
        bool hasRemoved = false;

        for (int row = 0; row < totalCatCount; row++)
        {
            if (row != rowIndex)
                hasRemoved |= RemoveCandidatesInRowOnly(candidatesPerRegion, row, allowLog, regionId);
        }
        return hasRemoved;
    }
    bool RemoveRegionExceptSingleColumn(List<Vector2Int>[] candidatesPerRegion, int columnIndex, char regionId, bool allowLog = true)
    {
        bool hasRemoved = false;

        for (int col = 0; col < totalCatCount; col++)
        {
            if (col != columnIndex)
                hasRemoved |= RemoveCandidatesInColumnOnly(candidatesPerRegion, col, allowLog, regionId);
        }
        return hasRemoved;
    }
    bool RemoveCandidatesByRegion(List<Vector2Int>[] candidatesPerRegion, Vector2Int catPos, bool allowLog = true)
    {
        char regionId = RegionUtility.GetRegionId(board, catPos);
        if (RegionUtility.TryGetIndexFromId(regionId, out int index))
        {
            if (candidatesPerRegion[index].Count > 0)
            {
                if (allowLog && logOnRemoveCandidate)
                    Debug.Log($"RemoveCandidatesByRegion :: {string.Join(", ", candidatesPerRegion[index])} / '{regionId}'");
                candidatesPerRegion[index].Clear();
                return true;
            }
        }
        return false;
    }

    #endregion

    #region

    bool HasFoundCat(int region)
    {
        if (hasFoundCatPerRegion == null)
        {
            Debug.LogError($"BoardSolver.HasFoundCat() Error :: hasFoundCatPerRegion is null");
            return false;
        }

        if (region < 0 || region >= hasFoundCatPerRegion.Length)
        {
            Debug.LogError($"BoardSolver.HasFoundCat() Error :: region ({region})이 hasFoundCatPerRegion 배열 범위 벗어남");
            return false;
        }

        return hasFoundCatPerRegion[region];
    }
    bool HasFoundCat(char regionId)
    {
        if (!RegionUtility.TryGetIndexFromId(regionId, out int region))
        {
            Debug.LogError($"BoardSolver.HasFoundCat() Error :: regionId '{regionId}'를 regionIndex로 변환 실패");
            return false;
        }    
            
        return HasFoundCat(region);
    }

    #endregion
}
