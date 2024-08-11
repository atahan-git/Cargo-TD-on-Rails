using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_RepairBomb : MonoBehaviour {

    public bool isArmed = false;
    public bool isExploded = false;
    void Update()
    {
        if (PlayerWorldInteractionController.s.currentSelectedThingMonoBehaviour == GetComponent<Artifact>()) {
            isArmed = true;
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (isArmed && !isExploded && PlayStateMaster.s.isCombatInProgress() && !GetComponent<Artifact>().isAttached) {
            var playerIsHoldingMe = PlayerWorldInteractionController.s.currentSelectedThingMonoBehaviour == GetComponent<Artifact>();
            //var myHolderDrone = GetComponent<Artifact>().GetHoldingDrone();
            var droneIsHoldingMe = false;
            
            if (!playerIsHoldingMe && !droneIsHoldingMe) {
                isExploded = true;
                Explode();
            }
        }
    }

    public void Explode() {
        for (int i = 0; i < Train.s.carts.Count; i++) {
            Train.s.carts[i].GetHealthModule().RepairChunk(1000);
        }

        if (LevelReferences.s.combatHoldableThings.Contains(GetComponent<Artifact>())) {
            LevelReferences.s.combatHoldableThings.Remove(GetComponent<Artifact>());
        }
        
        MiniGUI_Pick3GemReward.MakeGem(UpgradesController.s.potatoGemName, transform.position, transform.rotation);

        Destroy(gameObject);
    }
}
