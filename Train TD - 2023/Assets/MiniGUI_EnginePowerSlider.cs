using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_EnginePowerSlider : MonoBehaviour {
    public static MiniGUI_EnginePowerSlider s;

    public TMP_Text currentStatus;
    public Slider engineSlider;

    private void Awake() {
        s = this;
        GoBackToDefaultSpeed();
    }

    public int GetEngineSpeed() {
        return (int)engineSlider.value;
    }

    public void SetSpeed(int speed) {
        engineSlider.value = speed;
        OnValueChanged();
    }
    
    public void GoBackToDefaultSpeed() {
        engineSlider.value = 1;
    }

    public void OnValueChanged() {
        switch (engineSlider.value) {
            case 0:
                currentStatus.text = "Enemies Critical hit you when you are slow.\n" +
                                     "\n" +
                                     "If the train is stopped:\n" +
                                     "- Repair faster\n" +
                                     "- Reload easier";
                break;
            case 1:
                currentStatus.text = "Normal Operations";
                break;
            case 2:
                currentStatus.text = "Enemies' shots Miss when you are going fast.\n" +
                                     "\n" +
                                     "But also:\n" +
                                     "- Cannot reload\n" +
                                     "- Cannot repair\n" +
                                     "- Engine takes damage";
                break;
        }
    }
}
