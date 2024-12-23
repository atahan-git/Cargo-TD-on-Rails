using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_EncounterRequirementOrRewardDisplay : MonoBehaviour {


	public Image image;
	public TMP_Text amount;
	UITooltipDisplayer _uiITooltipDisplayer;
	
	public Color canMeetRequirementColor = Color.yellow;
	public Color cannotMeetRequirementColor = Color.red;
	public Color rewardColor = Color.green;
	
	public bool SetUp(EncounterRequirement requirement) {
		var canMeetRequirement = requirement.CanFulfillRequirement();

		image.sprite = requirement.icon;
		amount.text = requirement.amount.ToString();

		image.color = canMeetRequirement ? canMeetRequirementColor : cannotMeetRequirementColor;


		var myTooltipText = $"Use {requirement.amount} {requirement.myType} to pick this option";
		if (!canMeetRequirement) {
			myTooltipText += $"You don't have enough {requirement.myType}";
		}

		_uiITooltipDisplayer = GetComponent<UITooltipDisplayer>();
		_uiITooltipDisplayer.myTooltip.text = myTooltipText;


		return canMeetRequirement;
	}

	public void SetUp(EncounterReward reward) {
		_uiITooltipDisplayer = GetComponent<UITooltipDisplayer>();
		
		if (reward.damageTrain > 0) {
			image.sprite = reward.icon;
			image.color = cannotMeetRequirementColor;
			amount.text = "";
			var damageAmountText = "some";
			switch (reward.damageTrain) {
				case 1:
					damageAmountText = "a little";
					break;
				case 2:
					damageAmountText = "some";
					break;
				case 3:
					damageAmountText = "a lot of";
					break;
				case 4:
					damageAmountText = "so much";
					break;
			}
			_uiITooltipDisplayer.myTooltip.text = $"Your train will take {damageAmountText} damage";
			return;
		}
		
		//amount.text = reward.amount.ToString();

		/*if (reward.building == null) {
			image.sprite = reward.icon;
			_uiITooltipDisplayer.myTooltip.text = $"Get {reward.amount} {reward.myType}";
		} else {*/
		image.sprite = reward.building.Icon;
		_uiITooltipDisplayer.myTooltip.text = $"Get {reward.building.displayName}";
		//}

		amount.text = "";


		image.color = rewardColor;
	}
}
