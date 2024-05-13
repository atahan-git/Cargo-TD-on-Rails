using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_GemInfoSlot : MonoBehaviour {

	public Image icon;
	public TMP_Text title;
	public TMP_Text description;

	public GameObject artifactCard;
	public void SetUp(SnapLocation myLocation) {
		var artifact = myLocation.GetSnappedObject()?.GetComponent<Artifact>();

		
		artifactCard.SetActive(artifact != null);
		if (artifact != null) {
			icon.sprite = artifact.mySprite;
			title.text = artifact.displayName;
			description.text = artifact.GetDescription();
		} 
	}
}
