using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    [Range(4, 10)]
    public int repeatCount = 10;
    [Range(2, 9)]
    public int repeatDepth = 6;

    public Button testLogButton;


    private void Awake()
    {
        if (testLogButton != null)
            testLogButton.onClick.AddListener(TestMethod);
    }

    void TestMethod()
    {
        if (repeatDepth > repeatCount)
        {
            repeatDepth = repeatCount;
        }

        List<int> combination = new List<int>();

        Temp(repeatDepth,  combination);
    }

    void Temp(int repeatDepth, List<int> combination)
    {
        // 재귀 종료 조건
        if (combination.Count == repeatDepth)
        {
            Debug.Log($"Test Finish :: {repeatDepth}회 재귀 완료");
            return;
        }


        for (int i = 0; i < repeatCount; i++)
        {
            Debug.Log($"Log : {repeatCount} / {i}");
            combination.Add(i);
            Temp(repeatDepth + 1, combination);
        }
    }


    void TestMethod(int curRepeatDepth ,int targetRepeatDepth, int repeatCount)
    {
        if (curRepeatDepth == targetRepeatDepth)
            return;

        for(int i = 0; i < repeatCount; i++)
        {
            TestMethod(curRepeatDepth + 1, targetRepeatDepth, repeatCount);
        }
    }
}
