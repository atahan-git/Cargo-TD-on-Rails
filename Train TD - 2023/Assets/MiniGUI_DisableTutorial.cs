using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_DisableTutorial : MonoBehaviour, IInitRequired
{
    public const string exposedName = "tutorial";
    public Toggle myToggle;
    
    public void Initialize() {
        var toggleVal = PlayerPrefs.GetInt(exposedName, 1);
        var val = toggleVal == 1;
        myToggle.isOn = val;
        SetVal(val);
    }


    public void OnToggleUpdated() {
        SetVal(myToggle.isOn);
    }

    public static void SetVal(bool val) {
        PlayerPrefs.SetInt(exposedName, val ? 1 : 0);
        DataSaver.s.GetCurrentSave().tutorialProgress.showTutorials = !val;
    }

    public static bool IsTutorialActive() {
        return PlayerPrefs.GetInt(exposedName, 1) == 1;
    }
}