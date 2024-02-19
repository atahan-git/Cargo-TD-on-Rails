using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_TrackLever : MonoBehaviour {
    public GameObject lever;
    public GameObject upTrack;
    public GameObject bottomTrack;
    public GameObject button;

    public bool topSelected;

    public bool switchByColor = false;
    public Color disabledColor = Color.white;

    public GameObject buttonPrompt;
    public void SetButtonPromptState(bool state) {
        if(buttonPrompt != null)
            buttonPrompt.SetActive(state);
    }
    
    public void SetTrackState(bool state) {
        topSelected = state;

        if (topSelected) {
            if (switchByColor) {
                upTrack.SetActive(true);
                bottomTrack.SetActive(true);
                
                upTrack.transform.GetChild(0).GetComponent<Image>().color = Color.white;
                bottomTrack.transform.GetChild(0).GetComponent<Image>().color = disabledColor;
            } else {
                upTrack.SetActive(false);
                bottomTrack.SetActive(true);
            }

            var scale = Vector3.one;
            scale.x = -1;
            lever.transform.localScale = scale;
        } else {
            if (switchByColor) {
                upTrack.SetActive(true);
                bottomTrack.SetActive(true);
                
                upTrack.transform.GetChild(0).GetComponent<Image>().color = disabledColor;
                bottomTrack.transform.GetChild(0).GetComponent<Image>().color = Color.white;
            } else {
                upTrack.SetActive(true);
                bottomTrack.SetActive(false);
            }

            var scale = Vector3.one;
            //scale.x = -1;
            lever.transform.localScale = scale;
        }
    }

    public void LeverClicked() {
        if (!isLocked) {
            PathSelectorController.s.ActivateLever();
        }
    }

    public bool isLocked = false;
    public void LockTrackState() {
        isLocked = true;
        button.GetComponent<Button>().interactable = false;
        Destroy(buttonPrompt.gameObject);
    }


    public GameObject switchWarning;
    public Image warning1;
    public Image warning2;
    public void SetTrackSwitchWarningState(bool state) {
        enabled = state;
        switchWarning.SetActive(state);
        warningStateTimer = 0;
        Update();
    }


    public float warningStateTimer;
    private bool warningState;
    private void Update() {
        if (warningStateTimer <= 0) {
            warningState = !warningState;
            if (warningState) {
                warning1.color = Color.white;
                warning2.color = Color.red;
            } else {
                warning2.color = Color.white;
                warning1.color = Color.red;
            }

            warningStateTimer = 0.3f;
        } else {
            warningStateTimer -= Time.deltaTime;
        }
    }
}
