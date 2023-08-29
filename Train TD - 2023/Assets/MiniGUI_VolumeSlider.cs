using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MiniGUI_VolumeSlider : MonoBehaviour, IInitRequired
{
    public AudioMixer mixer;
    public string exposedName;
    public Slider mySlider;
    
    public void Initialize() {
        var audVal = PlayerPrefs.GetFloat(exposedName, 1f);
        mySlider.value = audVal;
        SetVol(audVal);
    }

    public void OnSliderUpdated() {
        PlayerPrefs.SetFloat(exposedName, mySlider.value);
        SetVol(mySlider.value);
    }

    void SetVol(float sliderVal) {
        sliderVal = Mathf.Clamp(sliderVal, 0.001f, 1f);

        // influencing FMOD mixer
        switch (exposedName) 
        {
            case "MasterVol":
                AudioManager.instance.masterBusVolume = sliderVal;
                mixer.SetFloat("MasterVol", Mathf.Log(sliderVal) * 20);
                break;
            case "MusicVol":
                AudioManager.instance.musicBusVolume = sliderVal;
                mixer.SetFloat("MusicVol", Mathf.Log(sliderVal) * 20);
                break;
            case "SFXVol":
                AudioManager.instance.targetUnitySfxVolume = Mathf.Log(sliderVal) * 20;
                AudioManager.instance.sfxBusVolume = sliderVal;
                break;
        }


        mixer.SetFloat(exposedName, Mathf.Log(sliderVal) * 20);
    }
}
