using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_ModifyLuck : ActivateWhenOnArtifactRow {


	[Tooltip("extra luck of 0.1 means getting epics and rares are both 10% more likely")]
	// and with high enough luck you will never get commons
	public float modifyAmount = 0;

	protected override void _Arm() {
		GetComponent<Artifact>().range = 100;
		DataSaver.s.GetCurrentSave().currentRun.luck += modifyAmount;
	}

	protected override void _Disarm() { 
		DataSaver.s.GetCurrentSave().currentRun.luck -= modifyAmount;
	}
	
}
