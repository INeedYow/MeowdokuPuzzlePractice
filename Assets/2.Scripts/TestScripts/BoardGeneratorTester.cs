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
        { 'a', 'a', 'a', 'b', 'b', 'b', 'b', 'b' },
        { 'a', 'a', 'a', 'a', 'c', 'b', 'b', 'b' },
        { 'd', 'd', 'd', 'a', 'c', 'e', 'e', 'e' },
        { 'd', 'f', 'f', 'a', 'c', 'c', 'g', 'g' },
        { 'd', 'f', 'f', 'a', 'c', 'c', 'g', 'g' },
        { 'd', 'f', 'f', 'a', 'a', 'c', 'h', 'h' },
        { 'f', 'f', 'h', 'h', 'a', 'h', 'h', 'h' },
        { 'f', 'h', 'h', 'h', 'h', 'h', 'h', 'h' }
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
