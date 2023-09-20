using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_PlayerActionAffector : ActivateWhenOnArtifactRow {
    public float repairAmountMultiplier = 1f;
    public float reloadAmountMultiplier = 1f;

    public bool makeCannotRepair;
    public bool makeCannotReload;
    public bool makeCannotSmith;
    
    protected override void _Arm() {
        //GetComponent<Artifact>().range = 100;
        PlayerWorldInteractionController.s.repairAmountMultiplier += repairAmountMultiplier -1;
        PlayerWorldInteractionController.s.reloadAmountMultiplier += reloadAmountMultiplier-1;

        if (makeCannotRepair)
            PlayerWorldInteractionController.s.canRepair = false;
        
        if (makeCannotRepair) {
            if (PlayerWorldInteractionController.s.canRepair) {
                PlayerWorldInteractionController.s.canRepair = false;
            } else {
                PlayerWorldInteractionController.s.autoRepairAtStation = false;
            }
        }
        
        if (makeCannotReload) {
            if (PlayerWorldInteractionController.s.canReload) {
                PlayerWorldInteractionController.s.canReload = false;
            } else {
                PlayerWorldInteractionController.s.autoReloadAtStation = false;
            }
        }


        if (makeCannotSmith)
            PlayerWorldInteractionController.s.canSmith = false;
    }

    protected override void _Disarm() {
        // do nothing
    }
}
