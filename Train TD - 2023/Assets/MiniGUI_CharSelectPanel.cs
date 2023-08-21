using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MiniGUI_CharSelectPanel : MonoBehaviour {
	public CharacterData myData;

	public TMP_Text charNameText;
	public TMP_Text charDescText;

	public GameObject lockedOverlay;

	public void Setup(CharacterData data, bool isLocked, bool autoSelect) {
		myData = data;
		charNameText.text = myData.uniqueName;
		charDescText.text = myData.description;
		
		lockedOverlay.SetActive(isLocked);
		GetComponentInChildren<Button>().interactable = !isLocked;

		if (autoSelect) {
			/*if (SettingsController.GamepadMode()) {
				EventSystem.current.SetSelectedGameObject(GetComponentInChildren<Button>().gameObject);
			}*/
		}
	}


	public void Select() {
		CharacterSelector.s.SelectCharacter(myData);
	}
}
