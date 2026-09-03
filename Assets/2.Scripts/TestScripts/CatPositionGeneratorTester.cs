using UnityEngine;
using UnityEngine.UI;

public class CatPositionGeneratorTester : MonoBehaviour
{
    public Board board;
    public Dropdown catCountDropdown;
    public Button generateButton;

    readonly int[] catCounts = { 4, 5, 6, 7, 8, 9, 10 };

    private void Awake()
    {
        if (generateButton != null)
            generateButton.onClick.AddListener(StartInitBoard);
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

}
