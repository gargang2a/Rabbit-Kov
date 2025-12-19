using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor; // 에디터 전용 네임스페이스
#endif

public class LoadingUIController : MonoBehaviour
{
    // ... (기존 ThemeUIObject 클래스 및 변수들은 동일) ...
    [System.Serializable]
    public class ThemeUIObject
    {
        public string name;
        public LoadingTheme theme;
        public GameObject rootObject;
        public Slider progressBar;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI tipText;
        [TextArea] public string[] tips;
    }

    [Header("Settings")]
    [SerializeField] private float _minLoadingTime = 2.0f;
    [SerializeField] private List<ThemeUIObject> _themeLayouts;

    // ---------------------------------------------------------
    // [개선된 부분] 테스트 모드 설정
    // ---------------------------------------------------------
    [Header("Debug / Test")]
    [Tooltip("체크하면 SceneLoader를 거치지 않고 바로 아래 설정된 씬을 로드합니다.")]
    [SerializeField] private bool _isTestMode = false;

#if UNITY_EDITOR
    // 에디터에서만 보이는 씬 에셋 필드 (드래그앤드롭 용도)
    [Tooltip("테스트할 씬 파일을 여기에 드래그하세요.")]
    [SerializeField] private SceneAsset _testSceneAsset;
#endif

    // 실제 로딩에 사용될 문자열 (Inspector에서 수정 불가하게 막음)
    [Tooltip("위의 씬 에셋을 넣으면 자동으로 이름이 입력됩니다.")]
    [SerializeField] private string _testSceneName;

    // ---------------------------------------------------------

    private Slider _currentSlider;
    private TextMeshProUGUI _currentProgressText;

    // [Editor Magic] 인스펙터 값이 변경될 때 호출되는 함수
    private void OnValidate()
    {
#if UNITY_EDITOR
        // 씬 에셋이 할당되어 있다면 이름을 자동으로 추출하여 문자열 변수에 저장
        if (_testSceneAsset != null)
        {
            string sceneName = _testSceneAsset.name;
            if (_testSceneName != sceneName)
            {
                _testSceneName = sceneName;
                // 변경 사항을 에디터에 즉시 반영 (Dirty Flag)
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }

    private void Start()
    {
        SetupLayout();
        StartCoroutine(LoadSceneProcess());
    }

    // ... (SetupLayout 함수는 기존과 동일) ...
    private void SetupLayout()
    {
        LoadingTheme currentTheme = SceneLoader.CurrentTheme;
        if (_isTestMode && currentTheme == default) currentTheme = LoadingTheme.Main;

        bool isLayoutFound = false;
        foreach (var layout in _themeLayouts)
        {
            if (layout.rootObject == null) continue;
            if (layout.theme == currentTheme)
            {
                ActivateTheme(layout);
                isLayoutFound = true;
            }
            else layout.rootObject.SetActive(false);
        }

        if (!isLayoutFound && _themeLayouts.Count > 0) ActivateTheme(_themeLayouts[0]);
    }

    private void ActivateTheme(ThemeUIObject layout)
    {
        layout.rootObject.SetActive(true);
        _currentSlider = layout.progressBar;
        _currentProgressText = layout.progressText;
        if (_currentSlider != null) _currentSlider.value = 0f;
        if (_currentProgressText != null) _currentProgressText.text = "0%";
        if (layout.tipText != null && layout.tips?.Length > 0)
            layout.tipText.text = layout.tips[Random.Range(0, layout.tips.Length)];
    }

    private IEnumerator LoadSceneProcess()
    {
        yield return Resources.UnloadUnusedAssets();
        System.GC.Collect();

        string targetScene = SceneLoader.TargetSceneName;

        // [수정] 테스트 모드 로직
        if (_isTestMode || string.IsNullOrEmpty(targetScene))
        {
            // 드래그앤드롭으로 추출된 이름을 사용
            targetScene = _testSceneName;
            Debug.Log($"[LoadingUI] Test Mode Active. Target Scene: '{targetScene}'");
        }

        // 유효성 검사
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[Critical] 로드할 씬 이름이 비어있습니다! 인스펙터에서 Test Scene Asset을 확인하세요.");
            if (_currentProgressText != null) _currentProgressText.text = "No Target Scene";
            yield break;
        }

        AsyncOperation op = null;
        try
        {
            op = SceneManager.LoadSceneAsync(targetScene);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Critical] 씬 로드 실패: {e.Message}");
        }

        if (op == null)
        {
            Debug.LogError($"[Error] '{targetScene}' 씬을 로드할 수 없습니다.\n" +
                           "1. File -> Build Settings에 해당 씬이 등록되어 있는지 꼭 확인하세요.\n" +
                           "2. 씬 파일 이름이 변경되었는지 확인하세요.");

            if (_currentProgressText != null) _currentProgressText.text = "Scene Not Found";
            yield break;
        }

        op.allowSceneActivation = false;
        float timer = 0f;

        while (!op.isDone)
        {
            yield return null;
            timer += Time.deltaTime;

            float actualProgress = op.progress;
            float timeProgress = Mathf.Clamp01(timer / _minLoadingTime);
            float targetValue = (actualProgress < 0.9f) ? actualProgress : timeProgress;

            if (_currentSlider != null)
            {
                _currentSlider.value = Mathf.Lerp(_currentSlider.value, targetValue, Time.deltaTime * 5f);
                if (_currentProgressText != null) _currentProgressText.text = $"{(int)(_currentSlider.value * 100)}%";

                if (actualProgress >= 0.9f && targetValue >= 1.0f && _currentSlider.value >= 0.99f)
                {
                    op.allowSceneActivation = true;
                }
            }
            else
            {
                if (actualProgress >= 0.9f && timer >= _minLoadingTime) op.allowSceneActivation = true;
            }
        }
    }
}