using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MiniGUI_GamepadSelectOnEnable : MonoBehaviour {
	private bool added = false;
	private void OnEnable() {
		if (SettingsController.GamepadMode()) {
			EventSystem.current.SetSelectedGameObject(gameObject);
		}

		if (SettingsController.s != null) {
			AddToGamepadButtons();
		} else {
			SettingsController.autoSelectDelay = 2;
			Invoke(nameof(AddToGamepadButtons),0.1f);
		}
	}

	void AddToGamepadButtons() {
		added = true;
		SettingsController.s.gamepadModeButtons.Add(gameObject);
	}

	private void OnDisable() {
		if (SettingsController.s != null) {
			if (added) {
				SettingsController.s.gamepadModeButtons.Remove(gameObject);
			}
		}
	}
}
