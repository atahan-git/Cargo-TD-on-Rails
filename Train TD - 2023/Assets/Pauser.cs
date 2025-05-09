using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class Pauser : MonoBehaviour {

    public static Pauser s;

    private void Awake() {
        s = this;
    }

    public InputActionReference pauseButton;

    public bool isPaused = false;
    public GameObject pauseMenu;

    private void OnApplicationFocus(bool hasFocus) {
        if(enabled && !Application.isEditor)
            if(!hasFocus)
                Pause();
    }

    private void OnApplicationPause(bool pauseStatus) {
        if(enabled && !Application.isEditor)
            if(pauseStatus)
                Pause();
    }

    private void OnEnable() {
        pauseButton.action.Enable();
        pauseButton.action.performed += TogglePause;
        
    }

    private void OnDisable() {
        pauseButton.action.Disable();
        pauseButton.action.performed -= TogglePause;
    }

    private void TogglePause(InputAction.CallbackContext obj) {
        var menuToggles = pauseMenu.GetComponentsInChildren<MenuToggle>();
        var menuClosed = false;

        for (int i = 0; i < menuToggles.Length; i++) {
            if (menuToggles[i].isMenuActive) {
                menuClosed = true;
                menuToggles[i].HideMenu();
            }
        }
        
        if(!menuClosed)
            TogglePause();
    }

    public bool blockPauseStateChange = false;
    void TogglePause() {
        if(blockPauseStateChange)
            return;
        
        isPaused = !isPaused;
        
        if (isPaused) {
            Pause();
        } else {
            Unpause();
        }
        
        //gamepadSelect.transition
    }


    private void Start() {
        Unpause();
    }


    [Button]
    public void Pause() {
        Pause(true);
    }

    private CursorLockMode modeBeforePause;
    public void Pause(bool showMenu) {
        if(blockPauseStateChange)
            return;
        
        AudioManager.PlayOneShot(SfxTypes.ButtonClick1);

        if(showMenu)
            pauseMenu.SetActive(true);
        
        TimeController.s.Pause();
        isPaused = true;
        
        //Debug.Break();

        modeBeforePause = Cursor.lockState;
        Cursor.lockState = CursorLockMode.None;
    }

    [Button]
    public void Unpause() {
        if(blockPauseStateChange)
            return;
        
        AudioManager.PlayOneShot(SfxTypes.ButtonClick2);
        pauseMenu.SetActive(false);
        TimeController.s.UnPause();
        isPaused = false;
        
        Cursor.lockState = modeBeforePause;
    }

    public void AbandonMission() {
        AnalyticsResult analyticsResult = Analytics.CustomEvent(
            "LevelAbandoned",
            new Dictionary<string, object> {
                { "Level", PlayStateMaster.s.currentLevel.levelName },
                { "distance", Mathf.RoundToInt(SpeedController.s.currentDistance / 10) *10},
                { "time", Mathf.RoundToInt(WorldDifficultyController.s.GetMissionTime()/10) * 10},

            }
        );
        
        Unpause();
        MissionLoseFinisher.s.MissionLost(MissionLoseFinisher.MissionLoseReason.abandon);
    }
}
