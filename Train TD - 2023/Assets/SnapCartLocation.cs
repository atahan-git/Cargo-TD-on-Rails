using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapCartLocation : MonoBehaviour {
    public Transform snapTransform;

    public UpgradesController.CartLocation myLocation;

    public bool snapNothing = false;
    public bool onlySnapCargo = false;
    public bool onlySnapMysteriousCargo = false;

    public GameObject emptyShowObject;

    public bool IsEmpty() {
        return snapTransform.childCount == 0;
    }
    public void SetEmptyStatus(bool isEmpty) {
        emptyShowObject.SetActive(isEmpty);
    }
    public void SnapToLocation(GameObject child) {
        child.transform.position = snapTransform.position;
        child.transform.rotation = snapTransform.rotation;
        child.transform.SetParent(snapTransform);
    }

    public float snapLerpSpeed = 5;
    public float snapSlerpSpeed = 20;
    private void Update() {
        if (snapTransform.childCount > 0) {
            var child = snapTransform.GetChild(0);
            
            child.transform.localPosition = Vector3.Lerp(child.transform.localPosition, Vector3.zero, snapLerpSpeed * Time.deltaTime);
            child.transform.localRotation = Quaternion.Slerp(child.transform.localRotation, Quaternion.identity, snapSlerpSpeed * Time.deltaTime);
        }
    }
}
