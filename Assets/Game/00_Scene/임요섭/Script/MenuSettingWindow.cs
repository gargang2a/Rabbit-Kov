using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class MenuSettingWindow : MonoBehaviour
{
    public Slider slider;
    public Image soundWaveImg;
    public List<Sprite> soundWaves;
    public void soundVolume()
    {
        if(slider.value == 0)
        {
            soundWaveImg.sprite = soundWaves[0];
        }
        else if(slider.value < 0.25)
        {
             soundWaveImg.sprite = soundWaves[1];
        }
        else if (slider.value < 0.5)
        {
            soundWaveImg.sprite = soundWaves[2];
        }
        else if (slider.value < 0.75)
        {
            soundWaveImg.sprite = soundWaves[3];
        }
    }
    public void CloseMenu()
    {
        gameObject.SetActive(false);
    }
    public void SaveSettingData()
    {
        print("Save");
        gameObject.SetActive(false);
    }
}
