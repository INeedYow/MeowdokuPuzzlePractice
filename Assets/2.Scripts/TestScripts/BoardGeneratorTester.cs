using UnityEngine;
using UnityEngine.UI;

public class BoardGeneratorTester : MonoBehaviour
{
    [Header("Components")]
    public Board board;
    public Dropdown catCountDropdown;
    public Button generateButton;

    [Header("테스트 보드 풀이 버튼")]
    public Button solveTestBoardButton;
    public BoardSolver solver;

    readonly int[] catCounts = { 4, 5, 6, 7, 8, 9, 10 };

    char[,] testBoard =
    {
        { 'A', 'a', 'b', 'b', 'b', 'b', 'c', 'c' },
        { 'a', 'a', 'b', 'B', 'b', 'b', 'c', 'c' },
        { 'a', 'a', 'b', 'b', 'b', 'b', 'b', 'C' },
        { 'f', 'f', 'f', 'f', 'D', 'b', 'b', 'b' },
        { 'f', 'f', 'f', 'f', 'd', 'e', 'E', 'b' },
        { 'f', 'F', 'f', 'f', 'e', 'e', 'b', 'b' },
        { 'f', 'f', 'f', 'f', 'g', 'G', 'b', 'b' },
        { 'h', 'h', 'H', 'h', 'g', 'g', 'b', 'b' }
    };

    private void Awake()
    {
        if (generateButton != null)
            generateButton.onClick.AddListener(StartInitBoard);

        if (solveTestBoardButton != null)
            solveTestBoardButton.onClick.AddListener(StartSolveTestBoard);
    }

    void StartInitBoard()
    {
        if (catCountDropdown == null)
            return;

        int index = catCountDropdown.value;

        if (index < 0 || index >= catCounts.Length)
            return;

        int catCount = catCounts[index];
        
        if (board == null)
            return;

        board.InitBoard(catCount);
    }


    void StartSolveTestBoard()
    {
        if (solver == null)
            return;

        if (testBoard.GetLength(0) != testBoard.GetLength(1))
        {
            Debug.LogError($"테스트 보드 풀이 Error :: TestBoard.GetLength(0) = {testBoard.GetLength(0)} / testBoard.GetLength(1) = {testBoard.GetLength(1)}");
            return;
        }
            
        int testCatCount = testBoard.GetLength(0);
        solver.Solve(testBoard, testCatCount);
    }
}
