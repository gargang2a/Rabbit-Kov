using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private int _topOrder = 10;

    private const int MAX_NORMAL_ORDER = 5000;
    public const int PAUSE_UI_ORDER = 10000;

    private bool _isAnyWindowOpen = false;
    public bool IsAnyWindowOpen => _isAnyWindowOpen;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void SetPausePriority(Canvas pauseCanvas)
    {
        if (pauseCanvas == null) return;
        pauseCanvas.sortingOrder = PAUSE_UI_ORDER;
    }
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