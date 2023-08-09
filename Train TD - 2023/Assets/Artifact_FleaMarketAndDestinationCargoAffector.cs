using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_FleaMarketAndDestinationCargoAffector : ActivateWhenOnArtifactRow {


    public int fleaMarketCountModifier = 0;
    public bool rewardDestinationArtifact = true;
    public bool rewardDestinationCart = true;
    
    protected override void _Arm() {
        GetComponent<Artifact>().range = 100;
        UpgradesController.s.fleaMarketLocationCount += fleaMarketCountModifier;

        if (!rewardDestinationArtifact) {
            UpgradesController.s.rewardDestinationArtifact = rewardDestinationArtifact;
        }
        
        if (!rewardDestinationCart) {
            UpgradesController.s.rewardDestinationCart = rewardDestinationCart; 
        }
    }

    protected override void _Disarm() {
        // do nothing
    }
}
