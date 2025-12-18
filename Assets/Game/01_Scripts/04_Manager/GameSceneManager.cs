using System.Collections;
using UnityEngine;
using UnityEngine.UI; // 슬라이더 사용을 위해 필수
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance;

    public enum SceneType
    {
        Main,       // 0
        BaseCamp,   // 1
        Battle,     // 2
        Boss        // 3
    }

    [Header("Scene Names (실제 씬 이름)")]
    public string mainSceneName = "00_Main";
    public string baseCampSceneName = "01_BaseCamp";
    public string battleSceneName = "02_World_Map_Making_Ulupdate";
    public string bossSceneName = "03_Boss";

    [Header("UI Reference")]
    public GameObject loadingScreen; // 검은 배경 패널
    public Slider progressBar;       // 로딩 게이지 슬라이더

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 넘어가도 파괴 안 됨
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ★ 버튼 연결용 함수 (인스펙터에서 숫자 입력: 0, 1, 2...)
    public void ChangeSceneByIndex(int sceneIndex)
    {
        ChangeScene((SceneType)sceneIndex);
    }

    // 내부 호출용 함수
    public void ChangeScene(SceneType sceneType)
    {
        StartCoroutine(LoadSceneRoutine(sceneType));
    }

    IEnumerator LoadSceneRoutine(SceneType sceneType)
    {
        // 1. 로딩 화면 켜기
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            if (progressBar != null) progressBar.value = 0f;
        }

        // 2. 목표 씬 이름 설정
        string targetSceneName = "";
        switch (sceneType)
        {
            case SceneType.Main: targetSceneName = mainSceneName; break;
            case SceneType.BaseCamp: targetSceneName = baseCampSceneName; break;
            case SceneType.Battle: targetSceneName = battleSceneName; break;
            case SceneType.Boss: targetSceneName = bossSceneName; break;
        }

        // 3. 비동기 로딩 시작
        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);
        op.allowSceneActivation = false; // 로딩 90%까지 대기

        float timer = 0.0f;

        // 4. 로딩 진행 루프
        while (!op.isDone)
        {
            yield return null;
            timer += Time.deltaTime;

            // [로딩 진행 중] (0% ~ 90%)
            if (op.progress < 0.9f)
            {
                if (progressBar != null)
                    progressBar.value = Mathf.Lerp(progressBar.value, op.progress, timer);

                if (progressBar.value >= op.progress) timer = 0f;
            }
            // [로딩 완료 단계] (90% ~ 100%)
            else
            {
                if (progressBar != null)
                    progressBar.value = Mathf.Lerp(progressBar.value, 1f, timer);

                // 게이지가 꽉 찼으면 씬 넘기기
                if (progressBar.value >= 0.99f)
                {
                    op.allowSceneActivation = true; // ★ 여기서 씬 전환 허용
                }
            }
        }

        // ★ [수정됨] 5. 씬 전환이 완전히 끝난 후 패널 끄기
        // (while 문을 빠져나온 뒤에 실행됨)
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }
}