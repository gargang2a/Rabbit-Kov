using DG.Tweening;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private RectTransform _pausePanel;
    [SerializeField] private Canvas _pauseCanvas;

    private bool _isPaused = false;

    private void Awake()
    {
        if (_pauseCanvas == null) _pauseCanvas = GetComponent<Canvas>();

        // ★ 수정: UIManager에 정의된 높은 순서를 할당
        _pauseCanvas.sortingOrder = UIManager.PAUSE_UI_ORDER;
        _pausePanel.gameObject.SetActive(false);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused) Resume();
            else TogglePause();
        }
    }
    private void OnEnable()
    {
        // 패널이 켜지는 순간 무조건 최고 우선순위 부여
        RefreshPriority();
    }
    public void TogglePause()
    {
        _isPaused = true;
        _pausePanel.gameObject.SetActive(true); // 활성화 시 OnEnable 실행됨
        transform.SetAsLastSibling();

        _pausePanel.gameObject.SetActive(true);
        Time.timeScale = 0f;

        _pausePanel.localScale = Vector3.zero;
        _pausePanel.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
    }
    private void RefreshPriority()
    {
        if (_pauseCanvas == null) _pauseCanvas = GetComponent<Canvas>();
        _pauseCanvas.sortingOrder = UIManager.PAUSE_UI_ORDER;

        // ★ 추가: 같은 캔버스 내에서도 맨 앞으로 가져오기
        transform.SetAsLastSibling();
    }
    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        _pausePanel.DOScale(0f, 0.2f).SetUpdate(true).OnComplete(() => {
            _pausePanel.gameObject.SetActive(false);
        });
    }
}