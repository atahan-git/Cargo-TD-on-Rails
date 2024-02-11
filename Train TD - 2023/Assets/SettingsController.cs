using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour {
    public static SettingsController s;

    private void Awake() {
        s = this;
        settingsParent.SetActive(false);
    }

    public GameObject settingsParent;

    public bool forceDisableGamepadMode = false;
    public bool forceEnableGamepadMode = false;
    
    public List<GameObject> gamepadModeButtons = new List<GameObject>();

    void Start()
    {
#if !UNITY_EDITOR
        forceDisableGamepadMode = false;
        forceEnableGamepadMode = false;
#endif
        
        var initRequiredSettings = settingsParent.GetComponentsInChildren<IInitRequired>();
        for (int i = 0; i < initRequiredSettings.Length; i++) {
            initRequiredSettings[i].Initialize();
        }
    }

    public GameObject areYouSureScreen;
    public TMP_Text areYouSureText;
    private Action areYouSureClick;

    private bool gamepadMode = false;
    
    //public GameObject mainUI;
    private void Update() {
        var currentGamepadMode = GamepadMode();
        if (currentGamepadMode != gamepadMode) {
            if (currentGamepadMode) {
                EventSystem.current.SetSelectedGameObject(gamepadModeButtons[^1]);
            }   
        }
        gamepadMode = currentGamepadMode;

        /*if (currentGamepadMode) {
            var currentSelected = EventSystem.current.currentSelectedGameObject;
            if (currentSelected == null) {
                var randomButton = mainUI.GetComponentInChildren<Button>();
                EventSystem.current.SetSelectedGameObject(randomButton.gameObject);
            }
        }*/
    }

    public void ResetRun() {
        /*if (DataSaver.s.GetCurrentSave().isInARun == true) {
            areYouSureScreen.SetActive(true);
            areYouSureText.text = "Really Abandon?";
            areYouSureClick = () => _ResetRun();

            //SFX
            AudioManager.PlayOneShot(SfxTypes.ButtonClick1);
        }*/
        
        
        AudioManager.PlayOneShot(SfxTypes.ButtonClick1);
    }

    void _ResetRun() {
        /*DataSaver.s.GetCurrentSave().currentRun = null;
        DataSaver.s.GetCurrentSave().isInARun = false;*/

        DataSaver.s.SaveActiveGame();

        SceneLoader.s.ForceReloadScene();
    }

    public void AreYouSureYes() {
        areYouSureClick?.Invoke();
        areYouSureScreen.SetActive(false);
        
        //SFX
        AudioManager.PlayOneShot(SfxTypes.ButtonClick1);
    }

    public void AreYouSureNo() {
        areYouSureScreen.SetActive(false);
        
        //SFX
        AudioManager.PlayOneShot(SfxTypes.ButtonClick1);
    }
    
    public void ResetRunAndReplayTutorial() {
        areYouSureScreen.SetActive(true);
        areYouSureText.text = "Really Play Tutorial?";
        areYouSureClick = () => _ResetRunAndReplayTutorial();

        //SFX
        AudioManager.PlayOneShot(SfxTypes.ButtonClick1);
    }
    
    void _ResetRunAndReplayTutorial() {
        MenuToggle.HideAllToggleMenus();
        Pauser.s.Unpause();
        
        FirstTimeTutorialController.s.ReDoTutorial();
        //ResetRun();

        //SFX
        AudioManager.PlayOneShot(SfxTypes.ButtonClick1);
    }
    public void ClearCurrentSaveAndPlayerPrefs() {
        PlayerPrefs.DeleteAll();
        DataSaver.s.ClearCurrentSave();
    }

    public void ReloadScene() {
        SceneLoader.s.ForceReloadScene();

        //SFX
        AudioManager.PlayOneShot(SfxTypes.ButtonClick1);
    }


    public static bool GamepadMode() {
        if (s != null)
            return (Gamepad.all.Count > 0 && !s.forceDisableGamepadMode) || s.forceEnableGamepadMode;
        else
            return Gamepad.all.Count > 0;
    }

    public static bool ShowButtonPrompts() {
        return MiniGUI_ShowButtonHints.ShowButtonHints();
    }

    public void StartAutoRunFast() {
        AutoPlaytester.s.StartAutoPlayer(true);
    }

    public void StartAutoRun() {
        AutoPlaytester.s.StartAutoPlayer(false);
    }
}

public interface IInitRequired {
    public void Initialize();
}
