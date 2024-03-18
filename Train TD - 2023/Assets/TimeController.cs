using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class TimeController : MonoBehaviour {
    public static TimeController s;

    private void Awake() {
        s = this;
        PausedEvent = new UnityEvent<bool>();
        debugAlwaysFastForward = false;
        debugDisableAbilityToFastForward = false;
    }


    public float currentTimeScale = 1f;
    public bool isPaused = false;

    public static UnityEvent<bool> PausedEvent = new UnityEvent<bool>();

    public void Pause() {
        isPaused = true;
        Time.timeScale = 0f;
        
        PausedEvent?.Invoke(true);
    }

    public void UnPause() {
        isPaused = false;
        Time.timeScale = currentTimeScale;
        
        PausedEvent?.Invoke(false);
    }


    public InputActionReference fastForwardKey;

    private void OnEnable() {
        fastForwardKey.action.Enable();
    }

    private void OnDisable() {
        fastForwardKey.action.Disable();
    }

    public bool debugAlwaysFastForward = false;

    public bool debugDisableAbilityToFastForward = false;
    private void Update() {
        if(!debugAlwaysFastForward)
            ProcessFastForward();
    }

    private bool canFastForward = false;
    public void OnCombatStart() {
        canFastForward = true;
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.fastForward);
    }

    public void OnCombatEnd(bool realCombat) {
        canFastForward = false;
        GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.fastForward);
    }

    public bool fastForwarding = false;
    public void ProcessFastForward() {
        //print(fastForwardKey.action.ReadValue<float>());
        if (fastForwardKey.action.IsPressed() && canFastForward && !DirectControlMaster.s.directControlInProgress && !debugDisableAbilityToFastForward) {
            currentTimeScale = 8f;
            if (!isPaused) {
                Time.timeScale = currentTimeScale;
            }

            fastForwarding = true;
        } else {
            currentTimeScale = 1f;
            if (!isPaused) {
                Time.timeScale = currentTimeScale;
            }
            
            fastForwarding = false;
        }
    }
}
