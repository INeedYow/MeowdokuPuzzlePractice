using UnityEngine;

public enum TileState 
{ 
    Empty           = 0,            // 기본 타일
    XMarked         = 1,            // 유저가 X 표시 (흰색 X)
    CatFound        = 2,            // 고양이 찾기 성공한 타일
    CatMissed       = 3,            // 고양이 찾기 실패한 타일 (빨간색 X)
}

// 퍼즐 규칙
    // 규칙1. 같은 색 타일에는 고양이 1마리만 존재
    // 규칙2. 각각의 행과 열에는 고양이 1마리만 존재
    // 규칙3. 고양이끼리는 인접(주위 8칸)할 수 없음

public class Board : MonoBehaviour
{
    // 정답 보드 : 각 색을 알파벳 소문자로, 고양이는 대문자로 표기
    char[,] answerBoard;
    // 플레이어 보드 : 빈 칸, X 표시, 고양이 찾음 표시
    TileState[,] playerBoard;

    int totalCatCount;
    int remainingCatCont;

    // 퍼즐 규칙에 맞는 보드 생성 알고리즘 아이디어
    // 1. 정답 고양이 위치 결정 (규칙2와 규칙3 해결)
    // 2. 타일 색 결정 후 유일한 정답인지 판별 후 유일한 정답이 아니면 폐기 (규칙1 해결)
    public void InitBoard(int totalcatCount)
    {
        this.totalCatCount = totalcatCount;
        this.remainingCatCont = totalcatCount;

        answerBoard = new char[totalcatCount, totalcatCount];
        playerBoard = new TileState[totalcatCount, totalcatCount];
    }
}
