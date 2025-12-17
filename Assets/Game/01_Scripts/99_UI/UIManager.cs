using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private int _topOrder = 10;

    // ★ 추가: 일반 UI는 5000번까지만 올라갈 수 있게 제한
    private const int MAX_NORMAL_ORDER = 5000;
    // ★ 추가: 일시정지 창은 무조건 이 번호보다 높게 설정
    public const int PAUSE_UI_ORDER = 10000;

    private bool _isAnyWindowOpen = false;
    public bool IsAnyWindowOpen => _isAnyWindowOpen;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    // ★ 추가: Pause UI 전용 함수
    // Pause가 켜질 때 이 함수를 호출하면 무조건 일반 UI보다 높은 10000번을 부여합니다.
    public void SetPausePriority(Canvas pauseCanvas)
    {
        if (pauseCanvas == null) return;
        pauseCanvas.sortingOrder = PAUSE_UI_ORDER;
    }
    // 일반 UI(인벤토리, 스탯 등)용: 호출될 때마다 1씩 증가
    public void BringToFront(Canvas canvas)
    {
        if (canvas == null) return;
        _topOrder++;
        if (_topOrder >= MAX_NORMAL_ORDER) _topOrder = 11;
        canvas.sortingOrder = _topOrder;
    }

    public void SetWindowStatus(bool isOpen)
    {
        _isAnyWindowOpen = isOpen;
    }
}