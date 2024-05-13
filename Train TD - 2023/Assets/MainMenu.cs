using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {
    public static MainMenu s;


    public GameObject[] goToDisableInProfileMenu;
    public MonoBehaviour[] monoToDisableInProfileMenu;
    public GameObject[] goToEnableInProfileMenu;
    public CameraSwitcher profileCameraSwithcer;


    [Space]
    
    public GameObject ProfileUI;
    public MonoBehaviour[] monoToEnableAfterProfileMenu;
    public MonoBehaviour[] monoToDisableAfterProfileMenu;
    public GameObject[] goToEnableAfterProfileMenu;
    public GameObject[] goToDisableAfterProfileMenu;

    public MiniGUI_ProfileDisplay currentProfile;
    public MenuToggle profileChangeMenu;


    public GameObject deleteSaveUI;

    private DataSaver.SaveFile saveToBeDeleted;
    public MiniGUI_ProfileDisplay deleteProfileDisplay;


    public DataSaver.TrainState profileScreenTrain;

    public bool drawDebugTrainInstead = false;
    public DataSaver.TrainState debugTrain;


    public TMP_Text startGameText;
    public GameObject resetButton;

    private void Awake() {
        s = this;
        deleteSaveUI.SetActive(false);
    }

    public void OpenProfileMenu() {
        for (int i = 0; i < monoToDisableInProfileMenu.Length; i++) {
            monoToDisableInProfileMenu[i].enabled = false;
        }

        for (int i = 0; i < goToDisableInProfileMenu.Length; i++) {
            goToDisableInProfileMenu[i].SetActive(false);
        }

        for (int i = 0; i < goToEnableInProfileMenu.Length; i++) {
            goToEnableInProfileMenu[i].SetActive(true);
        }

        profileCameraSwithcer.Engage();

        ProfileUI.SetActive(true);


        var isInARun = DataSaver.s.GetCurrentSave().isInARun;
        resetButton.gameObject.SetActive(isInARun);
        if (isInARun) {
            startGameText.text = "Continue";
        } else {
            startGameText.text = "Start";
        }

        if (SceneLoader.s.autoOpenProfiles) {
            profileChangeMenu.ShowMenu();
            SceneLoader.s.autoOpenProfiles = false;
        }
        
        if(drawDebugTrainInstead)
            Train.s.DrawTrain(debugTrain);
        else {
            if(!Train.s.isTrainDrawn)
                Train.s.DrawTrain(profileScreenTrain);
        }
        RangeVisualizer.SetAllRangeVisualiserState(false);
        
        /*if(SettingsController.GamepadMode())
            EventSystem.current.SetSelectedGameObject(ProfileUI.GetComponentInChildren<Button>().gameObject);*/
    }

    public void StartGame() {
        NewGameStarter.s.CheckStartNewGame();
        PlayStateMaster.s.EnterShopState();
    }

    public void QuickStartGame() {
        NewGameStarter.s.CheckStartNewGame();
        PlayStateMaster.s.EnterShopState();
    }

    public void ExitMainMenu() {
        profileCameraSwithcer.Disengage();
        ProfileUI.SetActive(false);

        for (int i = 0; i < monoToEnableAfterProfileMenu.Length; i++) {
            monoToEnableAfterProfileMenu[i].enabled = true;
        }
        
        for (int i = 0; i < goToEnableAfterProfileMenu.Length; i++) {
            goToEnableAfterProfileMenu[i].SetActive(true);
        }
        
        for (int i = 0; i < goToDisableAfterProfileMenu.Length; i++) {
            goToDisableAfterProfileMenu[i].SetActive(false);
        }
        
        for (int i = 0; i < monoToDisableAfterProfileMenu.Length; i++) {
            monoToDisableAfterProfileMenu[i].enabled = false;
        }
    }

    public void SaveChanged() {
        SceneLoader.s.autoOpenProfiles = true;
        SceneLoader.s.ForceReloadScene();
        //currentProfile.SetStats(DataSaver.s.GetCurrentSave());
    }

    public void DeleteSaveDialog(DataSaver.SaveFile saveFile) {
        deleteSaveUI.SetActive(true);
        deleteProfileDisplay.SetStats(saveFile);
    }

    public void DeleteConfirm() {
        deleteSaveUI.SetActive(false);
        DataSaver.s.ClearCurrentSave();
        /*SaveChanged();
        ProfileUI.SetActive(false);
        ProfileUI.SetActive(true);*/
        SaveChanged();
    }

    public void DeleteCancel() {
        deleteSaveUI.SetActive(false);
    }


    public void QuitGame() {
        Application.Quit();
    }
}