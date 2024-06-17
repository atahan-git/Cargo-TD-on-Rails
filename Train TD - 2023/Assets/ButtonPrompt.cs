using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ButtonPrompt : MonoBehaviour {
    public GamepadControlsHelper.PossibleActions myAction;

    public InputActionReference myActionReference;

    public bool gamepadModeOnly = false;
    public bool keyboardModeOnly = false;

    public Image keyPrompt;
    
    [HideIf("keyboardModeOnly")]
    public Sprite gamepadSprite;
    [HideIf("gamepadModeOnly")]
    public Sprite keyboardSprite;

    
    public bool alwaysActive = false;

    public void SetState(bool isOn, bool gamepadMode) {
        var gamepadHide = (gamepadModeOnly && !gamepadMode);
        var keyboardHide = (keyboardModeOnly && gamepadMode);
        
        if ((isOn && !(gamepadHide || keyboardHide)) || alwaysActive) {
            gameObject.SetActive(true);
        } else {
            gameObject.SetActive(false);
            return;
        }

        if (gamepadMode) {
            keyPrompt.sprite = gamepadSprite;
        } else {
            keyPrompt.sprite = keyboardSprite;
        }
    }

    private void Start() {
        GamepadControlsHelper.s.buttonPrompts.Add(this);
        GamepadControlsHelper.s.UpdateButtonPrompts();
    }

    private void OnDestroy() {
        if (GamepadControlsHelper.s != null)
            GamepadControlsHelper.s.buttonPrompts.Remove(this);
        
    }


    [Button]
    void Debug_ApplyGamepadSprite() {
        keyPrompt.sprite = gamepadSprite;
    }
    
    [Button]
    void Debug_ApplyKeyboardSprite() {
        keyPrompt.sprite = keyboardSprite;
    }
}
