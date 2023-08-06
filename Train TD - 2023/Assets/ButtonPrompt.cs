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

    public Image keyPrompt;
    
    [HideIf("gamepadModeOnly")]
    public Sprite gamepadSprite;
    [HideIf("gamepadModeOnly")]
    public Sprite keyboardSprite;

    public void SetState(bool isOn, bool gamepadMode) {
        if (!isOn || (gamepadModeOnly && !gamepadMode)) {
            gameObject.SetActive(false);
            return;
        } else {
            gameObject.SetActive(true);
        }

        if (!gamepadModeOnly) {
            if (gamepadMode) {
                keyPrompt.sprite = gamepadSprite;
            } else {
                keyPrompt.sprite = keyboardSprite;
            }
        }
    }

    private void Start() {
        GamepadControlsHelper.s.buttonPrompts.Add(this);
        GamepadControlsHelper.s.UpdateButtonPrompts();
    }

    private void OnDestroy() {
        if(GamepadControlsHelper.s != null)
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
