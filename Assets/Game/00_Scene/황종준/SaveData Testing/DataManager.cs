using UnityEngine;
using System.IO; // ★ 파일 입출력(File, Path)을 위해 필수
using System.Text; // (선택) 인코딩 관련

public class DataManager : MonoBehaviour
{
    public static DataManager instance;

    [Header("Current Play Data")]
    public GameData currentGameData; // 현재 메모리에 올라와 있는 데이터

    // 저장될 파일 이름
    private string _saveFileName = "SaveFile.json";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ================================================================
    // 1. 새 게임 시작 (New Game)
    // ================================================================
    public void InitNewGame()
    {
        currentGameData = new GameData();

        // 초기값 설정 (필요하다면)
        // currentGameData._currentHp = 100f;

        Debug.Log("[DataManager] 새로운 게임 데이터를 생성했습니다.");
    }

    // ================================================================
    // 2. 게임 저장 (Save) - 기지로 돌아올 때 호출
    // ================================================================
    public void SaveGame()
    {
        if (currentGameData == null)
        {
            Debug.LogWarning("[DataManager] 저장할 데이터가 없습니다!");
            return;
        }

        // 1. 데이터를 JSON(문자열)으로 변환
        string json = JsonUtility.ToJson(currentGameData, true); // true: 보기 좋게 줄바꿈

        // 2. 저장 경로 설정 (PC, 모바일 모두 작동하는 안전한 경로)
        string path = Path.Combine(Application.persistentDataPath, _saveFileName);

        // 3. 파일로 쓰기
        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"[DataManager] 저장 완료! 경로: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DataManager] 저장 실패: {e.Message}");
        }
    }

    // ================================================================
    // 3. 게임 불러오기 (Load) - 메인 메뉴 '이어하기' 때 호출
    // ================================================================
    public bool LoadGame()
    {
        string path = Path.Combine(Application.persistentDataPath, _saveFileName);

        // 1. 파일이 존재하는지 확인
        if (!File.Exists(path))
        {
            Debug.Log("[DataManager] 세이브 파일이 없습니다.");
            return false; // 파일 없음
        }

        // 2. 파일 읽기 및 데이터 복구
        try
        {
            string json = File.ReadAllText(path);
            currentGameData = JsonUtility.FromJson<GameData>(json);

            Debug.Log("[DataManager] 불러오기 성공!");
            return true; // 성공
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DataManager] 불러오기 실패(파일 깨짐 등): {e.Message}");
            return false;
        }
    }
}