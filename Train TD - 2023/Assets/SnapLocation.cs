using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapLocation : MonoBehaviour {
    public enum AllowedSnaps {
        nothing, cart, cargoCart, regularArtifact, cartAndRegularArtifact, regularAndComponentArtifact
    }

    public AllowedSnaps myAllowedSnaps = AllowedSnaps.nothing;

    public Transform snapTransform;
    public GameObject visualizeEffect;

    public float snapDistance = 1f;
    public bool lerpChild = true;

    private void Start() {
        LevelReferences.s.allSnapLocations.Add(this);
        if (snapTransform == null) {
            snapTransform = transform;
        }
    }

    private void OnDestroy() {
        LevelReferences.s.allSnapLocations.Remove(this);
    }

    public bool IsEmpty() {
        return snapTransform.childCount == 0;
    }

    float snapLerpSpeed => PlayerWorldInteractionController.lerpSpeed;
    float snapSlerpSpeed => PlayerWorldInteractionController.slerpSpeed;
    private void Update() {
        if (lerpChild) {
            if (snapTransform.childCount > 0) {
                var child = GetSnappedObject();

                var isArtifact = child.GetComponent<Artifact>() != null;

                var offset = Vector3.zero;

                if (isArtifact) {
                    offset = Vector3.up * (3 / 4f);
                }

                child.transform.localPosition = Vector3.Lerp(child.transform.localPosition, offset, snapLerpSpeed * Time.deltaTime);
                child.transform.localRotation = Quaternion.Slerp(child.transform.localRotation, Quaternion.identity, snapSlerpSpeed * Time.deltaTime);
            }
        }
    }

    public bool CanSnap(IPlayerHoldable thing) {
        switch (myAllowedSnaps) {
            case AllowedSnaps.nothing:
                return false;
            case AllowedSnaps.cart:
                return thing is Cart;
            case AllowedSnaps.cargoCart:
                var myCart = thing as Cart;
                if (myCart == null) {
                    return false;
                } else {
                    return myCart.GetComponentInChildren<CargoModule>() != null;
                }
            case AllowedSnaps.regularArtifact: 
            {
                var myArtifact = thing as Artifact;

                if (!myArtifact)
                    return false;

                return !myArtifact.isComponent;
            }
            case AllowedSnaps.cartAndRegularArtifact: 
            {
                if (thing is Cart)
                    return true;

                var myArtifact = thing as Artifact;
                if (!myArtifact)
                    return false;

                return !myArtifact.isComponent;
            }
            case AllowedSnaps.regularAndComponentArtifact:
                return thing is Artifact;
            default:
                return false;
        }
    }

    public Transform GetSnappedObject() {
        if (snapTransform.childCount > 0) {
            return snapTransform.GetChild(0);
        } else {
            return null;
        }
    }

    public void SnapObject(GameObject gameObject) {
        gameObject.transform.SetParent(snapTransform);
    }

    public void SetVisualizeState(bool state) {
        if (visualizeEffect != null) {
            visualizeEffect.SetActive(state);
        }
    }
}
