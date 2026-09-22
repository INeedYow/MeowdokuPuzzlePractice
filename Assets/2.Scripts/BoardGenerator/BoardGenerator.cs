using UnityEngine;
using System.Collections.Generic;

public class BoardGenerator : MonoBehaviour
{
    [Header("")]
    [SerializeField] CatPositionGenerator catPositionGenerator;
    [SerializeField] TileColorGenerator tileColorGenerator;
    [SerializeField] BoardSolver boardSolver;

    public char[,] Generate(int catCount)
    {
        if (catPositionGenerator == null)
        {
            Debug.LogError($"BoardGenerate Error :: catPositionGenerator is null");
            return null;
        }
        if (tileColorGenerator == null)
        {
            Debug.LogError($"BoardGenerate Error :: tileColorGenerator is null");
            return null;
        }
        if (boardSolver == null)
        {
            Debug.LogError($"BoardGenerate Error :: boardSolver is null");
            return null;
        }

        List<Vector2Int> catPositions;
        char[,] newBoard;
        int generateCount = 0;
        do
        {
            generateCount++;

            // 고양이 위치 결정
            catPositions = catPositionGenerator.Generate(catCount);

            // 타일 색상 결정
            newBoard = tileColorGenerator.Generate(catCount, catPositions);
        } while (!boardSolver.Solve(newBoard, catCount));

        Debug.Log($"BoardGenerator :: 풀이 가능한 보드 {generateCount} 회 시도에 생성 완료");
        return newBoard;
    }
}
