using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_PlayerActionAffector : ActivateWhenOnArtifactRow {
    public bool makeCannotSmith;
    
    protected override void _Arm() {
        if (makeCannotSmith)
            PlayerWorldInteractionController.s.canSmith = false;
    }

    protected override void _Disarm() {
        // do nothing
    }
}
