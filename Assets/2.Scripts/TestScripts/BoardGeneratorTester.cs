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
        { 'b', 'b', 'b', 'b', 'A', 'a', 'a', 'a', 'a', 'f' },
        { 'b', 'B', 'b', 'b', 'a', 'a', 'a', 'a', 'a', 'f' },
        { 'b', 'b', 'b', 'b', 'a', 'c', 'C', 'c', 'f', 'f' },
        { 'b', 'b', 'd', 'D', 'b', 'b', 'c', 'f', 'f', 'f' },
        { 'b', 'b', 'b', 'b', 'b', 'b', 'e', 'E', 'f', 'f' },
        { 'b', 'b', 'g', 'b', 'b', 'b', 'e', 'e', 'e', 'F' },
        { 'b', 'b', 'G', 'b', 'b', 'h', 'e', 'h', 'h', 'i' },
        { 'b', 'b', 'b', 'b', 'b', 'H', 'h', 'h', 'h', 'i' },
        { 'b', 'b', 'b', 'b', 'b', 'b', 'i', 'i', 'I', 'i' },
        { 'J', 'j', 'j', 'b', 'b', 'b', 'b', 'b', 'i', 'i' }
    };
    int testCatCount = 10;

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

        solver.Solve(testBoard, testCatCount);
    }
}
