using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_LuckGem : ActivateWhenOnArtifactRow, IResetStateArtifact {


    [Tooltip("extra luck of 0.1 means getting epics and rares are both 10% more likely")]
    // and with high enough luck you will never get commons
    public float modifyAmount = 0.05f;
    public float modifyIncreasePerLevel = 0.025f;
    public float curModifyAmount = 0;

    protected override void _Arm() {
        DataSaver.s.GetCurrentSave().currentRun.luck += modifyAmount;
        var range = GetComponent<Artifact>().range;
        ApplyBoost(Train.s.GetNextBuilding(0, GetComponentInParent<Cart>()));
        for (int i = 1; i < range+1; i++) {
            ApplyBoost(Train.s.GetNextBuilding(i, GetComponentInParent<Cart>()));
            ApplyBoost(Train.s.GetNextBuilding(-i, GetComponentInParent<Cart>()));
        }
    }
    
    void ApplyBoost(Cart target) {
        if(target == null)
            return;

        bool didApply = true;
        target.GetHealthModule().luckyCart = true;

        if (didApply) {
            GetComponent<Artifact>()._ApplyToTarget(target);
        }
    }

    protected override void _Disarm() { 
        //do nothing
    }

    public void ResetState(int level) {
        curModifyAmount = modifyAmount + (modifyIncreasePerLevel * curModifyAmount);
    }
}
