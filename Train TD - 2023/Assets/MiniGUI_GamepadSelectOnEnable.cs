using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MiniGUI_GamepadSelectOnEnable : MonoBehaviour
{
	private void OnEnable() {
		if (SettingsController.GamepadMode()) {
			EventSystem.current.SetSelectedGameObject(gameObject);
		}
	}
}
