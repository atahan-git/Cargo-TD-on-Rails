using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_CapacityAndSpeedAffector : ActivateWhenOnArtifactRow {
    public int capacityBoost = 1;
    public float speedMultiplier = 0;
    public float speedAmount = 0;

    public bool delicateMachinery = false;

    protected override void _Arm() {
        //GetComponent<Artifact>().range = 100;
        SpeedController.s.cartCapacityModifier += capacityBoost;
        SpeedController.s.speedMultiplier += speedMultiplier;
        SpeedController.s.speedAmount += speedAmount;

        if (delicateMachinery)
            SpeedController.s.delicateMachinery = true;
    }

    protected override void _Disarm() {
        // dont do anything, before disarming speed controller resets everyting anyways
    }
}
