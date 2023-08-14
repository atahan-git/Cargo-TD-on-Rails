using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_RepairCapacityBar : MonoBehaviour
{
	
	public RectTransform mainRect;
	public RectTransform healthBar;
	public Image healthFill;
	
	private static readonly int Tiling = Shader.PropertyToID("_Tiling");

	private void Start() {
		healthFill.material = new Material(healthFill.material);
	}

	private void Update() {
		SetRepairBarValue();
	}
	
	void SetRepairBarValue() {
		var maxVal = PlayerWorldInteractionController.s.maxRepairCapacity;
		var curVal = PlayerWorldInteractionController.s.curRepairCapacity;

		var percent = curVal/maxVal;
		percent = Mathf.Clamp(percent, 0, 1f);

		var totalLength = mainRect.sizeDelta.x;
        
		healthBar.SetRight(totalLength*(1-percent));
		healthFill.material.SetFloat(Tiling, curVal/100f);
	}
}
