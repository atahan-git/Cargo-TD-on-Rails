using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGUI_TutorialPanel : MonoBehaviour {

    public float activeTime = 0;

    private void OnEnable() {
        activeTime = 0;
    }

    void Update() {
        activeTime += Time.deltaTime;
        if (PlayerWorldInteractionController.s.hideTutorial.action.WasPerformedThisFrame()) {
            HidePanel();
        }
    }


    public void HidePanel() {
        if (activeTime > 0.5f) {
            gameObject.SetActive(false);
        }
    }
}
