using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class TrainBuildingTweakableApplier : MonoBehaviour
{
	private void Start() {
		//ApplyStats(DataHolder.s.GetTweaks());
		TweakablesMaster.s.tweakableChanged.AddListener(ApplyStats);
		ApplyStats();
	}

	
	public void ApplyStats() {
		var cart = GetComponent<Cart>();
		var trainBuilding = GetComponent<Cart>();
		var health = GetComponent<ModuleHealth>();
	}
}
