using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class MinGUI_InfoCard_TimeRelic : MonoBehaviour , IBuildingInfoCard {
    public TMP_Text time;

    private int hour;
    private int minute;
    private float second;

    private void Start() {
        minute = Random.Range(0, 20);
        second = Random.Range(0, 60);
    }

    public void SetUp(Cart building) {
        gameObject.SetActive(false);
    }

    public void SetUp(EnemyHealth enemy) {
        gameObject.SetActive(false);
    }

    public void SetUp(Artifact artifact) {
        if (artifact.uniqueName == "starter_artifact") {
            gameObject.SetActive(true);
            SetTime();
        } else {
            gameObject.SetActive(false);
        }
    }

    void SetTime() {
        if (CharacterSelector.s.isInCharSelect) {
            hour = 7;
        }else if (PlayStateMaster.s.isShop()) {
            hour = 10;
        }else if (PlayStateMaster.s.isCombatInProgress()) {
            hour = 15;
        }else if (PlayStateMaster.s.isEndGame()) {
            hour = 18;
        }
    }


    private void Update() {
        second += Time.deltaTime;

        if (second >= 60) {
            minute += 1;
            second -= 60;
            if (minute >= 60) {
                minute -= 60;
            }
        }

        time.text = $"Time: {hour:00}:{minute:00}:{second:00}";
    }
}