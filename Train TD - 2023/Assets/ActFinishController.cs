using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActFinishController : MonoBehaviour {
    public static ActFinishController s;

    private void Awake() {
        s = this;
    }

    private void Start() {
        CloseActUI();
    }

    public GameObject act1WinUI;
    public GameObject act2WinUI;
    public GameObject act3WinUI;
    public void OpenActWinUI() {
        PlayerWorldInteractionController.s.canSelect = false;
        if (DataSaver.s.GetCurrentSave().currentRun.currentAct == 1) {
            act1WinUI.SetActive(true);
            EventSystem.current.SetSelectedGameObject(act1WinUI.GetComponentInChildren<Button>().gameObject);
        }else if (DataSaver.s.GetCurrentSave().currentRun.currentAct == 2) {
            act2WinUI.SetActive(true);
            EventSystem.current.SetSelectedGameObject(act2WinUI.GetComponentInChildren<Button>().gameObject);
        } else {
            act3WinUI.SetActive(true);
            EventSystem.current.SetSelectedGameObject(act3WinUI.GetComponentInChildren<Button>().gameObject);
        }
    }


    private bool movingToNextAct = false;
    public void StartNewAct() {
        PlayerWorldInteractionController.s.canSelect = true;
        movingToNextAct = true;
        
        if (DataSaver.s.GetCurrentSave().currentRun.currentAct == 3) {
            DataSaver.s.GetCurrentSave().currentRun = null;
            DataSaver.s.GetCurrentSave().isInARun = false;
            DataSaver.s.SaveActiveGame();
            ShopStateController.s.BackToMainMenu();
            return;
        }
        
        DataSaver.s.GetCurrentSave().currentRun.currentAct += 1;

        DataSaver.s.SaveActiveGame();
        PlayStateMaster.s.EnterNewAct();
    }

    public void CloseActUI() {
        movingToNextAct = false;
        act1WinUI.SetActive(false);
        act2WinUI.SetActive(false);
        act3WinUI.SetActive(false);
    }
}
