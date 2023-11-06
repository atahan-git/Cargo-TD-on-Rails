using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_ShowButtonHints : MonoBehaviour, IInitRequired
{
    public const string exposedName = "buttonHints";
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

    void SetVal(bool val) {
        PlayerPrefs.SetInt(exposedName, val ? 1 : 0);
    }

    public static bool ShowButtonHints() {
        return PlayerPrefs.GetInt(exposedName, 1) == 1;
    }
}