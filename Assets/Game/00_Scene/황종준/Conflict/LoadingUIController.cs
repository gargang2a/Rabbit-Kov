using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LoadingUIController : MonoBehaviour
{
    // [Inspector 디자인] 테마별 설정을 한눈에 보기 좋게 그룹화
    [System.Serializable]
    public class ThemeUIObject
    {
        [Header("Theme Identity")]
        [Tooltip("이 테마를 식별할 이름 (예: Green Theme)")]
        public string name;

        [Tooltip("SceneLoader에서 전달받을 테마 타입")]
        public LoadingTheme theme;

        [Header("UI References")]
        [Tooltip("이 테마의 최상위 부모 오브젝트 (이게 꺼지면 하위 UI 다 꺼짐)")]
        public GameObject rootObject;

        [Space(5)] // 약간의 여백
        public Slider progressBar;
        public TextMeshProUGUI progressText;

        [Space(5)]
        public TextMeshProUGUI tipText;

        [Header("Resources")]
        public AudioSource audioSource;

        [Tooltip("로딩 중 랜덤하게 표시될 팁 문구들")]
        [TextArea(3, 5)] // 텍스트 입력창을 3~5줄 크기로 확보
        public string[] tips;
    }

    // ---------------------------------------------------------

    [Header("1. General Settings")]
    [Tooltip("로딩이 너무 빨리 끝나도 이 시간만큼은 로딩 화면을 유지합니다.")]
    [SerializeField, Range(0f, 10f)] private float _minLoadingTime = 2.0f;

    [Header("2. Theme Configurations")]
    [Tooltip("하나의 로딩 씬 안에 여러 테마의 UI(Root Object)를 등록하세요.\n로직에 따라 적절한 테마가 자동으로 켜집니다.")]
    [SerializeField] private List<ThemeUIObject> _themeLayouts;

    // ---------------------------------------------------------

    [Header("3. Debug / Test Mode")]
    [Tooltip("체크 시: SceneLoader 없이 단독 테스트 가능")]
    [SerializeField] private bool _isTestMode = false;

    // [이전 피드백 반영] 테스트 모드일 때 보여줄 테마 선택 기능
    [Tooltip("테스트 모드일 때 강제로 띄울 UI 테마를 선택하세요.")]
    [SerializeField] private LoadingTheme _testTheme = LoadingTheme.MainToBattle;

#if UNITY_EDITOR
    [Space(10)]
    [SerializeField] private SceneAsset _testSceneAsset;
#endif

    [Tooltip("자동 입력됨 (수정 불필요)")]
    [SerializeField] private string _testSceneName;

    private Slider _currentSlider;
    private TextMeshProUGUI _currentProgressText;
    private AudioSource _currentAudioSource;

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (_testSceneAsset != null)
        {
            string sceneName = _testSceneAsset.name;
            if (_testSceneName != sceneName)
            {
                _testSceneName = sceneName;
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }

    private void Start()
    {
        SetupLayout(); // 1. 초기화 및 캐싱
        StartCoroutine(LoadSceneProcess()); // 2. 로딩 프로세스 시작
    }

    private void SetupLayout()
    {
        LoadingTheme currentTheme = SceneLoader.CurrentTheme;

        // [수정 1] 테스트 모드일 때, 인스펙터에서 지정한 _testTheme를 사용하도록 변경
        if (_isTestMode)
        {
            currentTheme = _testTheme;
            Debug.Log($"[LoadingUI] Test Mode Theme Active: {currentTheme}");
        }
        else if (currentTheme == default)
        {
            // 실제 플레이인데 테마가 지정 안 됐을 경우의 방어 코드
            currentTheme = LoadingTheme.MainToBattle;
        }

        bool isLayoutFound = false;
        foreach (var layout in _themeLayouts)
        {
            if (layout.rootObject == null) continue;

            if (layout.theme == currentTheme)
            {
                ActivateTheme(layout);
                isLayoutFound = true;
            }
            else
            {
                layout.rootObject.SetActive(false);
            }
        }

        // 해당하는 테마가 없으면 0번이라도 킴 (Fallback)
        if (!isLayoutFound && _themeLayouts.Count > 0)
        {
            ActivateTheme(_themeLayouts[0]);
        }
    }

    private void ActivateTheme(ThemeUIObject layout)
    {
        layout.rootObject.SetActive(true);
        _currentSlider = layout.progressBar;
        _currentProgressText = layout.progressText;
        _currentAudioSource = layout.audioSource; // 오디오 소스 캐싱

        if (_currentSlider != null) _currentSlider.value = 0f;
        if (_currentProgressText != null) _currentProgressText.text = "0%";

        // 팁 텍스트 설정
        if (layout.tipText != null && layout.tips != null && layout.tips.Length > 0)
            layout.tipText.text = layout.tips[Random.Range(0, layout.tips.Length)];

        // 오디오 재생 시작 (설정되어 있다면)
        if (_currentAudioSource != null && !_currentAudioSource.isPlaying)
        {
            _currentAudioSource.Play();
        }
    }

    private IEnumerator LoadSceneProcess()
    {
        yield return null; // 한 프레임 대기 (UI 렌더링 안정화)
        yield return Resources.UnloadUnusedAssets(); // 메모리 정리
        System.GC.Collect();

        string targetScene = SceneLoader.TargetSceneName;
        Debug.Log($"[LoadingUI] 목표 씬 이름: {targetScene}");

        if (_isTestMode || string.IsNullOrEmpty(targetScene))
        {
            targetScene = _testSceneName;
        }

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[Critical] Target Scene is Empty.");
            yield break;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        if (op == null)
        {
            Debug.LogError($"[Critical] '{targetScene}' 씬을 찾을 수 없습니다! Build Settings에 등록되었나요?");
            yield break;
        }

        op.allowSceneActivation = false;
        float timer = 0f;

        while (!op.isDone)
        {
            yield return null;
            timer += Time.deltaTime;

            float actualProgress = op.progress; // 0.0 ~ 0.9
            float timeProgress = Mathf.Clamp01(timer / _minLoadingTime);

            // 시각적 진행률 (실제 로딩과 시간 중 더 작은 쪽을 따라감 -> 너무 빨리 로딩돼도 _minLoadingTime은 지킴)
            float visualProgress = (actualProgress < 0.9f) ? actualProgress : timeProgress;

            if (_currentSlider != null)
            {
                _currentSlider.value = Mathf.Lerp(_currentSlider.value, visualProgress, Time.deltaTime * 5f);
                if (_currentProgressText != null)
                    _currentProgressText.text = $"{(int)(_currentSlider.value * 100)}%";
            }

            // [로딩 완료 조건]
            // 1. 실제 로딩이 90% 이상 (Unity는 0.9가 로딩 완료 시점)
            // 2. 최소 로딩 시간 경과 (visualProgress가 1.0 도달)
            // 3. 슬라이더 UI가 꽉 찼는지 확인
            bool isReady = actualProgress >= 0.9f && visualProgress >= 1.0f;
            if (_currentSlider != null) isReady &= (_currentSlider.value >= 0.99f);

            if (isReady)
            {
                // [수정 2] 씬 전환 직전에 로딩 화면의 오디오를 끕니다.
                if (_currentAudioSource != null)
                {
                    // 부드럽게 끄고 싶다면 여기서 코루틴으로 볼륨을 줄여도 되지만,
                    // 확실한 처리를 위해 Stop()을 호출합니다.
                    _currentAudioSource.Stop();
                }

                op.allowSceneActivation = true;
            }
        }
    }
}