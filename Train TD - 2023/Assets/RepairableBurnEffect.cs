using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairableBurnEffect : MonoBehaviour {

    public bool canRepair = true;

    public bool isTaken = false;

    public bool hasArrow = false;
    public GameObject arrow;
    public float removeArrowState = 0;
    public float requiredPullOutAmount = 0.25f;
    
    public void Repair() {
        canRepair = false;
        GetComponentInChildren<Collider>().gameObject.SetActive(false);
        GetComponent<SmartDestroy>().Engage();

        VisualEffectsController.s.SmartInstantiate(LevelReferences.s.repairDoneEffect, transform.parent, transform.position, transform.rotation);
    }


    public void SetRemoveArrowState(float state) {
        removeArrowState = state;
        arrow.transform.localPosition = removeArrowState * Vector3.forward * requiredPullOutAmount;
    }

    public void RemoveArrow() {
        hasArrow = false;
        arrow.transform.SetParent(VisualEffectsController.s.transform);
        arrow.AddComponent<Rigidbody>().drag = 0.1f;
        arrow.GetComponent<Rigidbody>().velocity = Train.s.GetTrainForward() * LevelReferences.s.speed;
        arrow.AddComponent<RubbleFollowFloor>();
        arrow = null;
    }
}
