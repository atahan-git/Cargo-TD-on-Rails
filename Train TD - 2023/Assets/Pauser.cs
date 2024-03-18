using System;
using System.Collections;
using System.Collections.Generic;
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

    void TogglePause() {
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


    public void Pause() {
        AudioManager.PlayOneShot(SfxTypes.ButtonClick1);

        pauseMenu.SetActive(true);
        TimeController.s.Pause();
        isPaused = true;
        
        //Debug.Break();
        
        if (CameraController.s.directControlActive) {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void Unpause() {
        AudioManager.PlayOneShot(SfxTypes.ButtonClick2);
        pauseMenu.SetActive(false);
        TimeController.s.UnPause();
        isPaused = false;
        
        if (CameraController.s.directControlActive) {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void AbandonMission() {
        AnalyticsResult analyticsResult = Analytics.CustomEvent(
            "LevelAbandoned",
            new Dictionary<string, object> {
                { "Level", PlayStateMaster.s.currentLevel.levelName },
                { "distance", Mathf.RoundToInt(SpeedController.s.currentDistance / 10) *10},
                { "time", Mathf.RoundToInt(WorldDifficultyController.s.GetMissionTime()/10) * 10},

                { "enemiesLeftAlive", EnemyHealth.enemySpawned - EnemyHealth.enemyKilled},
            }
        );
        
        Unpause();
        FirstTimeTutorialController.s.RemoveAllTutorialStuff();
        MissionLoseFinisher.s.MissionLost(MissionLoseFinisher.MissionLoseReason.abandon);
    }
}
