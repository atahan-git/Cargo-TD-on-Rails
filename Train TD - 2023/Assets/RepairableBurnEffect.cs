using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairableBurnEffect : MonoBehaviour {

    public bool canRepair = true;

    public bool isTaken = false;

    public void Repair() {
        canRepair = false;
        GetComponentInChildren<Collider>().gameObject.SetActive(false);
        GetComponent<SmartDestroy>().Engage();

        VisualEffectsController.s.SmartInstantiate(LevelReferences.s.repairDoneEffect, transform.parent, transform.position, transform.rotation);
    }
}
