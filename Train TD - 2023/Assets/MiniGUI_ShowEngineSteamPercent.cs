using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_ShowEngineSteamPercent : MonoBehaviour {

    [ReadOnly]
    public EngineModule engineModule;
    [ReadOnly]
    public Image myImg;
    void Start() {
        engineModule = GetComponentInParent<EngineModule>();
        myImg = GetComponent<Image>();
    }

    private float flashPercent;
    void Update() {
        if (!PlayStateMaster.s.isCombatInProgress()) {
            myImg.fillAmount = 0;
            myImg.color = Color.white;
            return;
        }

        var pressure = engineModule.currentPressure;
        var pressurePercent = pressure / 3f;

        if (pressurePercent > 0.05f) {
            myImg.fillAmount = Mathf.MoveTowards(myImg.fillAmount, pressurePercent, 1*Time.deltaTime);

            var color = Color.green;
            if (pressure < engineModule.greenZone[0]) {
                color = Color.cyan;
            } else if (pressure < engineModule.greenZone[1]) {
                color = Color.green;
            } else if (pressure < engineModule.pressureDropRanges[1]) {
                color = Color.cyan;
            } else if (pressure < engineModule.pressureDropRanges[2]) {
                color = Color.yellow;
            } else {
                color = Color.red;
            }

            myImg.color = Color.Lerp(myImg.color, color, 5 * Time.deltaTime);
        } else {
            myImg.fillAmount = Mathf.MoveTowards(myImg.fillAmount, 1, 1*Time.deltaTime);
            flashPercent += 0.5f*Time.deltaTime;
            flashPercent %= 1;
            var red = new Color(0.7f,0,0);
            var grey = new Color(0.2f, 0, 0);
            var color = red;
            if (flashPercent < 0.5f) {
                var colorPercent = flashPercent * 2;
                color = Color.Lerp(grey, red, colorPercent);
            } else {
                
                var colorPercent = (flashPercent-0.5f) * 2;
                color = Color.Lerp(red, grey, colorPercent);
            }

            myImg.color = Color.Lerp(myImg.color, color, 5 * Time.deltaTime);
        }
    }
}
