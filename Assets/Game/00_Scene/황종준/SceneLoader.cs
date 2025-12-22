using UnityEngine;
using UnityEngine.SceneManagement;

// 로딩 화면의 테마를 결정하는 열거형
// 보스전에서는 Battle의 데이터를 파괴하면 안 되므로
// Boss enum을 따로 만들지 않습니다.
public enum LoadingTheme
{
    MainToBattle,
    BaseToMain,
    BattleToBase,
    BaseToBattle,
    Dead
}

public static class SceneLoader
{
    // 데이터 전달용 프로퍼티
    public static string TargetSceneName = "배틀씬";
    public static LoadingTheme CurrentTheme = LoadingTheme.MainToBattle;

    // [Standard] 일반적인 씬 전환 (A -> 로딩씬 -> B)
    // 메모리를 완전히 정리하고 이동합니다. (Main <-> BaseCamp <-> Battle)
    public static void LoadScene(string sceneName, LoadingTheme theme)
    {
        TargetSceneName = sceneName;
        CurrentTheme = theme;

        // 로딩 전용 씬 호출 (Build Settings에 등록 필수)
        SceneManager.LoadScene("LoadingScene");
    }

    // [Special] 보스전 진입 (Battle 씬 유지 + Boss 씬 추가)
    // 로딩 씬을 거치지 않고 바로 위에 얹습니다 (Additive).
    public static void LoadBossAdditive(string bossSceneName)
    {
        // 주의: UI 페이드 아웃/인 효과는 BattleScene 내부 UI 매니저가 처리해야 함
        SceneManager.LoadSceneAsync(bossSceneName, LoadSceneMode.Additive);
    }
}