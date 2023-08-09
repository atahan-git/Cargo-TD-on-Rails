using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_TrainBoostModifier : ActivateWhenOnArtifactRow {

	public bool engineBoostDamageInstead = false;

	protected override void _Arm() {
		GetComponent<Artifact>().range = 100;
		if (engineBoostDamageInstead) {
			PlayerWorldInteractionController.s.engineBoostDamageInstead = true;
		}
	}

	protected override void _Disarm() {
		// do nothing
	}
}