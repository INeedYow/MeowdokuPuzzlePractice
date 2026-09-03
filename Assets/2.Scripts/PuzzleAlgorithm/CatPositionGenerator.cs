using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CatPositionGenerator : MonoBehaviour
{
    int totalRowCount;

    // 0 : 고양이 위치 가능
    // 1 이상 : 고양이 위치 불가능
    // -1 (CatValue) : 고양이
    int[,] unavailableCount;
    const int CatValue = -1;

    // 현재 row의 배치가능한 고양이 위치
    List<int> columnIndexCandidates;

    // 이전 고양이 위치 선택이 잘못되어 백트래킹하는 경우,
    // 현재 탐색 경로에서 이미 시도했지만 이후 Row 배치에 실패한 후보의 Column을 저장
    // rowIndex = r의 고양이 위치가 변경되면 하위 Row(r+1 ~ N-1)의 failedCandidates 초기화 필요
    //
    // [예시 - 4x4 board]
    // -1   2   2   2
    //  2   2  -1   1
    //  1   1   1   1  ← rowIndex 2에서 배치 불가능
    //  1   0   1   0
    //
    // → rowIndex 1의 고양이 위치(-1)를 failedCandidates에 저장하고,
    //   rowIndex 1에서 다른 위치로 변경하여 다시 탐색
    List<int>[] failedCandidates;


    public List<Vector2Int> Generate(int catCount)
    {
        totalRowCount = catCount;

        unavailableCount = new int[catCount, catCount];
        failedCandidates = new List<int>[catCount];
        for (int i = 0; i < failedCandidates.Length; i++)
            failedCandidates[i] = new List<int>();

        DecideCatInRow(0);

        
        return ExtractCatPositions();
    }

    // 아이디어
    // 제일 위 row부터 배치 가능한 위치 중 무작위로 고양이 배치
    // 다음 row에서 반복(재귀)하되, 퍼즐 규칙에 맞게 배치가 불가능하면 이전 row의 배치된 고양이를 다른 위치로 변경
        // 이때 배치 불가능한 상위 레벨 고양이 위치를 failedCandidates에 따로 관리하며, 이 위치들도 고양이 위치 선택 시 후보에서 제외
        // failedCandidates는 상위 row 고양이 위치가 변경(선택)될 때 하위 row의 failedCandidates 초기화
    // 모든 row에서 배치하면 종료
    void DecideCatInRow(int rowIndex)
    {
        // 배치 가능 -> 배치하고 재귀
        if (TryExtractCandidates(rowIndex))
        {
            // 무작위 선택
            int randomIndex = Random.Range(0, columnIndexCandidates.Count);
            int columnIndex = columnIndexCandidates[randomIndex];

            // 배치
            PlaceCat(new Vector2Int(columnIndex, rowIndex));
            // 하위 Row들 FailedCandidates 초기화
            ClearLowerRowFailedCandidates(rowIndex);

            // 모든 고양이 배치하면 재귀 종료
            rowIndex++;
            if (rowIndex >= totalRowCount)
                return;
        }
        // 배치 불가능 -> 이전 row의 배치된 위치를 불가능으로 바꾸고 이전 row로 재귀
        else
        {
            rowIndex--;

            if (rowIndex < 0)
            {
                Debug.LogError($"CatPositionGenerator Error :: 배치 실패");
                return;
            }

            // 이전 row 고양이 제거
            if (TryGetCatPositionInRow(rowIndex, out Vector2Int lastRowCatPos))
            {
                // 배치 제거
                RemoveCat(lastRowCatPos);
                // 이전 row 고양이 위치를 failedCandidate에 추가
                AddFailedCandidate(lastRowCatPos);
            }
            else
            {
                Debug.LogError($"CatPositionGenerator Error :: 이전 Row({rowIndex})의 고양이 찾기 실패");
                return;
            }
        }

        // 재귀
        DecideCatInRow(rowIndex);
    }

    bool TryGetCatPositionInRow(int rowIndex, out Vector2Int catPos)
    {
        catPos = new Vector2Int(0, rowIndex);
        for (int x = 0; x < unavailableCount.GetLength(1); x++)
        {
            if (unavailableCount[rowIndex, x] == CatValue)
            {
                catPos.x = x;
                return true;
            }
        }
        return false;
    }

    bool TryExtractCandidates(int rowIndex)
    {
        columnIndexCandidates = new List<int>();
        for (int x = 0; x < unavailableCount.GetLength(1); x++)
        {
            // 규칙에 의해 배치 불가능한 위치 제외
            if (unavailableCount[rowIndex, x] != 0)
                continue;

            // failedCandidates에 의해 배치 불가능한 위치 제외
            if (failedCandidates[rowIndex].Contains(x))
                continue;

            columnIndexCandidates.Add(x);
        }
        return columnIndexCandidates.Count > 0;
    }

    void PlaceCat(Vector2Int position)
    {
        if (unavailableCount[position.y, position.x] == CatValue)
        {
            Debug.LogError($"{position} 좌표에 이미 고양이 배치되었는데 다시 배치를 시도해서 무시함");
            return;
        }

        unavailableCount[position.y, position.x] = CatValue;

        // Column 규칙 적용
        for (int y = 0; y < totalRowCount; y++)
        {
            if (y == position.y) continue;
            unavailableCount[y, position.x]++;
        }
        // Row 규칙 적용
        for (int x = 0; x < totalRowCount; x++)
        {   
            if (x == position.x) continue;
            unavailableCount[position.y, x]++;
        }
        // 인접(대각 4칸) 규칙 적용
        if (position.x - 1 >= 0)
        {
            if (position.y - 1 >= 0)
                unavailableCount[position.y - 1, position.x - 1]++;
            if (position.y + 1 <  totalRowCount)
                unavailableCount[position.y + 1, position.x - 1]++;
        }
        if (position.x + 1 < totalRowCount)
        {
            if (position.y - 1 >= 0)
                unavailableCount[position.y - 1, position.x + 1]++;
            if (position.y + 1 < totalRowCount)
                unavailableCount[position.y + 1, position.x + 1]++;
        }
    }

    void RemoveCat(Vector2Int position)
    {
        if (unavailableCount[position.y, position.x] != CatValue)
        {
            Debug.LogError($"{position} 좌표에 고양이가 없는데 제거를 시도해서 무시함");
            return;
        }

        unavailableCount[position.y, position.x] = 0;

        // Column 규칙 적용
        for (int y = 0; y < totalRowCount; y++)
        {
            if (y == position.y) continue;
            unavailableCount[y, position.x]--;
        }
        // Row 규칙 적용
        for (int x = 0; x < totalRowCount; x++)
        {
            if (x == position.x) continue;
            unavailableCount[position.y, x]--;
        }
        // 인접(대각 4칸) 규칙 적용
        if (position.x - 1 >= 0)
        {
            if (position.y - 1 >= 0)
                unavailableCount[position.y - 1, position.x - 1]--;
            if (position.y + 1 < totalRowCount)
                unavailableCount[position.y + 1, position.x - 1]--;
        }
        if (position.x + 1 < totalRowCount)
        {
            if (position.y - 1 >= 0)
                unavailableCount[position.y - 1, position.x + 1]--;
            if (position.y + 1 < totalRowCount)
                unavailableCount[position.y + 1, position.x + 1]--;
        }
    }

    void AddFailedCandidate(Vector2Int position)
    {
        failedCandidates[position.y].Add(position.x);
    }
    void ClearLowerRowFailedCandidates(int rowIndex)
    {
        for (int i = rowIndex + 1; i < failedCandidates.Length; i++)
            failedCandidates[i].Clear();
    }


    List<Vector2Int> ExtractCatPositions()
    {
        List<Vector2Int> catPositions = new List<Vector2Int>();

        for (int y = 0; y < unavailableCount.GetLength(0); y++)
        {
            for (int x = 0; x < unavailableCount.GetLength(1); x++)
            {
                if (unavailableCount[y, x] == -1)
                    catPositions.Add(new Vector2Int(x, y));
            }
        }

        // Debug
        if (catPositions.Count != totalRowCount)
            Debug.LogError($"CatPositionGenerator Error :: 목표 고양이 수 ({totalRowCount}) != 생성된 고양이 수 ({catPositions.Count})");
        else
            Debug.Log($"CatPositionGenerator Success :: 목표 고양이 수 ({totalRowCount}) = 생성된 고양이 수 ({catPositions.Count})");

        DebugLog_Result(catPositions);

        return catPositions;
    }

    void DebugLog_Result(List<Vector2Int> catPositions)
    {
        Debug.Log("----[Cat Positions]----");
        foreach (var pos in catPositions)
            Debug.Log(pos);
        Debug.Log("-------------------------");
        Debug.Log("----[UnavailableCount]----");
        StringBuilder sb = new StringBuilder();

        for (int y = 0; y < unavailableCount.GetLength(0); y++)
        {
            for (int x = 0; x < unavailableCount.GetLength(1); x++)
                sb.Append($"{unavailableCount[y, x],3} ");

            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
        Debug.Log("-------------------------");
    }

}
