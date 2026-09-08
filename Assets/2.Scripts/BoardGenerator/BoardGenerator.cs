using UnityEngine;

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

        // 고양이 위치 결정
        var catPositions = catPositionGenerator.Generate(catCount);

        if (tileColorGenerator == null)
        {
            Debug.LogError($"BoardGenerate Error :: tileColorGenerator is null");
            return null;
        }

        // 타일 색상 결정
        var newBoard = tileColorGenerator.Generate(catCount, catPositions);

        if (boardSolver == null)
        {
            Debug.LogError($"BoardGenerate Error :: boardSolver is null");
            return null;
        }

        boardSolver.Solve(newBoard, catCount);

        return newBoard;
    }
}
