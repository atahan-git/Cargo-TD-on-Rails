using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    
    private void Awake() {
        s = this;
    }

    private void Start() {
        deleteSaveUI.SetActive(false);
        if (PlayStateMaster.s.isMainMenu()) {
            OpenProfileMenu();
        } else {
            OpenProfileMenu(); // Because we need this to disable some stuff for us
            StartGame();
        }
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
    }

    public void StartGame() {
        FirstTimeTutorialController.s.TutorialCheck();
        PlayStateMaster.s.EnterShopState();
    }

    public void QuickStartGame() {
        //SceneLoader.s.DebugQuickBackToStarterMenu();
        //DoTransfer();
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