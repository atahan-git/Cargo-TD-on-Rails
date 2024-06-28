using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_RepairBomb : MonoBehaviour {

    public bool isArmed = false;
    void Update()
    {
        if (PlayerWorldInteractionController.s.currentSelectedThingMonoBehaviour == GetComponent<Artifact>()) {
            isArmed = true;
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (isArmed && PlayStateMaster.s.isCombatInProgress()) {
            var playerIsHoldingMe = PlayerWorldInteractionController.s.currentSelectedThingMonoBehaviour == GetComponent<Artifact>();
            var myHolderDrone = GetComponent<Artifact>().GetHoldingDrone();
            var droneIsHoldingMe = false;
            if (myHolderDrone != null) {
                if (myHolderDrone.caughtCarry) {
                    droneIsHoldingMe = true;
                }
            }
            if (!playerIsHoldingMe && !droneIsHoldingMe) {
                Explode();
            }
        }
    }

    public void Explode() {
        for (int i = 0; i < Train.s.carts.Count; i++) {
            Train.s.carts[i].GetHealthModule().RepairChunk(1000);
        }
        
        
        MiniGUI_Pick3GemReward.MakeGem(UpgradesController.s.potatoGemName, transform.position, transform.rotation);

        Destroy(gameObject);
    }
}
