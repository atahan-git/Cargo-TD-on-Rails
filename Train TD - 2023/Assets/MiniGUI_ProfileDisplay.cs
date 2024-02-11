using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class MiniGUI_ProfileDisplay : MonoBehaviour {

    public TMP_Text fuelText;
    public TMP_Text scrapsText;
    public TMP_Text moneyText;
    public TMP_Text ammoText;
    public TMP_Text xpText;
    public TMP_Text profileName;
    public TMP_Text playTime;

    public bool autoSetOnStart = true;

    private DataSaver.SaveFile mySave;

    public MiniGUI_ProfilePowerUpDisplay[] myProfilePowerUpDisplays;
    void Start() {
        if (autoSetOnStart) {
            SetStats(DataSaver.s.GetCurrentSave());
        }
    }


    private void Update() {
        if (/*mySave.isInARun*/false) {
            /*profileName.text = mySave.currentRun.character.uniqueName;
            playTime.text = SpeedController.GetNiceTime(mySave.currentRun.playtime);
            
            scrapsText.text = $"{MoneyController.s.scraps:F0}";

            xpText.text = mySave.metaProgress.castlesTraveled.ToString();

            for (int i = 0; i < mySave.currentRun.powerUps.Count; i++) {
                myProfilePowerUpDisplays[i].UpdatePowerUpDisplay(mySave.currentRun.powerUps[i]);
            }*/
        } else {
            moneyText.text = "0";
            profileName.text = "yeet";
            playTime.text = SpeedController.GetNiceTime(0);
            
            fuelText.text = $"-";
            scrapsText.text = $"-";
            ammoText.text = $"-";
            
            for (int i = 0; i < myProfilePowerUpDisplays.Length; i++) {
                myProfilePowerUpDisplays[i].UpdatePowerUpDisplay("");
            }
        }
    }

    public void SetStats(DataSaver.SaveFile save) {
        mySave = save;
    }
}
