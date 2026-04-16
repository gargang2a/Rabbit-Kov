using UnityEngine;
using UnityEngine.SceneManagement;

// [Role] 배틀 씬의 주요 흐름(시작, 플레이어 사망, 재시작)을 관리합니다.
public class BattleSceneManager : MonoBehaviour
{
    // 이 스크립트는 씬에 하나만 존재해야 합니다.
    private void Awake()
    {
        // 안전장치: 혹시 이전에 구독된 이벤트가 남아있으면 초기화
        Player.OnPlayerDeathSequenceCompleted -= HandlePlayerDeath;
        // "플레이어 사망" 이벤트를 구독합니다.
        Player.OnPlayerDeathSequenceCompleted += HandlePlayerDeath;
    }

    private void Start()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayGameBGM();
        }
        else
        {
            Debug.LogError("[BattleSceneManager] SoundManager가 없습니다!");
        }
    }

    private void OnDestroy()
    {
        // ★ 중요: 이 오브젝트가 파괴될 때 구독을 반드시 해제해야 메모리 누수(Leak)를 막습니다.
        Player.OnPlayerDeathSequenceCompleted -= HandlePlayerDeath;
    }

    // 플레이어 사망 이벤트가 방송되면 자동으로 호출될 함수
    private void HandlePlayerDeath()
    {
        // 이 로그가 안 뜨면 Player.cs에서 이벤트 발송 코드가 지워진 것입니다.
        Debug.Log("[BattleSceneManager] Player death event received. Restarting scene...");
        RestartScene();
    }

    private void RestartScene()
    {
        Time.timeScale = 1.0f;
        // ★ [추가] 로딩 씬으로 넘어가기 직전에 BGM과 환경음을 끕니다.
        if (SoundManager.instance != null) SoundManager.instance.StopAllLoopSound();

        // 1. 현재 씬의 이름을 가져옵니다.
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 2. SceneLoader를 통해 "목적지"를 "현재 씬"으로 설정합니다.
        SceneLoader.TargetSceneName = currentSceneName;
        // 필요하다면 로딩 테마도 설정할 수 있습니다.
        SceneLoader.CurrentTheme = LoadingTheme.Dead;

        // 3. 로딩 씬으로 이동하여 재시작 프로세스를 시작합니다.
        SceneManager.LoadScene("04_Loading_Death"); // 로딩 씬 이름은 실제 프로젝트에 맞게 수정
    }
}