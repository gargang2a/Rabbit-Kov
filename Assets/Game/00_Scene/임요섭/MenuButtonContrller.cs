using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtonContrller : MonoBehaviour
{
    [SerializeField]
    public GameObject SettingWindow;
    public string MainMenuScene = "00_Main Menu";
    public string newGameScene = "CharacterCreate";
    public string continueScene = "CharacterLord";
    public string BasehouseScene = "01_Base House";
    public string BattleScene = "02_Battle";
    private Vector3 defaultScale;
    public float clickScaleFactor = 0.9f;
    void Start()
    {
        defaultScale = transform.localScale;
        if(SettingWindow != null)
        {
            SettingWindow.SetActive(false);
        }
    }
    public void Settings()
    {
        if(SettingWindow != null)
        {
            SettingWindow.SetActive(true);
        }
    }
    public void CloseSettings()
    {
        if(SettingWindow != null)
        {
            SettingWindow.SetActive(false);
        }
    }
    public void MainMenuScene_(string MainMenu)
    {
        SceneManager.LoadScene("00_Main Menu");
    }
    public void CharacterCreateScene(string CharacterCreate)
    {
        SceneManager.LoadScene("CharacterCreate");
    }
    public void CharacterLordScene(string CharacterCreate)
    {
        SceneManager.LoadScene("CharacterLord");
    }
    public void BasehouseScene_(string Basehouse)
    {
        SceneManager.LoadScene("01_Base House");
    }
    public void BattleScene_(string BattleScene)
    {
        SceneManager.LoadScene("02_Battle");
    }
    public void QuitScene(string Quit)
    {
        UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit();
    }
}
