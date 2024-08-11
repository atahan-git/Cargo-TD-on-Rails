using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrapsItem : MonoBehaviour, IPlayerHoldable {

    public enum ScrapsType {
        red, blue, green, colorless
    }

    public GameObject redScrapsGfx;
    public GameObject blueScrapsGfx;
    public GameObject greenScrapsGfx;

    public ScrapsType myType;

    public bool canHold = true;

    public bool CanDrag() {
        return !PlayStateMaster.s.isCombatInProgress() && canHold;
    }

    private void Start() {
        SetScrapsType(myType);
    }


    public Color GetSelectColor() {
        switch (myType) {
            case ScrapsType.red:
                return Color.red;
            case ScrapsType.blue:
                return Color.blue;
            case ScrapsType.green:
                return Color.green;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetScrapsType(ScrapsType toSet) {
        myType = toSet;
        redScrapsGfx.SetActive(myType == ScrapsType.red);
        blueScrapsGfx.SetActive(myType == ScrapsType.blue);
        greenScrapsGfx.SetActive(myType == ScrapsType.green);
    }
    
    public Transform GetUITargetTransform() {
        return transform;
    }

    public void SetHoldingState(bool state) {
        if (state) {
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Rigidbody>().useGravity = false;
        } else {
            GetComponent<Rigidbody>().isKinematic = false;
            GetComponent<Rigidbody>().useGravity = true;
        }
    }
}
