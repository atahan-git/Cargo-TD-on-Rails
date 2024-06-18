using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapLocation : MonoBehaviour {
    public enum AllowedSnaps {
        nothing=0, cart=1, regularArtifact=2, cartAndRegularArtifact=3, regularAndComponentArtifact=4, scrapable=6, red=7, blue=8, green=9, anyColor=10, justComponentArtifact = 11
    }

    public AllowedSnaps myAllowedSnaps = AllowedSnaps.nothing;

    public Transform snapTransform;
    public GameObject visualizeEffect;

    public float snapDistance = 1f;
    public bool lerpChild = true;

    public bool allowSnap = true;

    [HideInInspector]
    public float curDistance;
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


                var offset = Vector3.zero;

                if (myAllowedSnaps == AllowedSnaps.cartAndRegularArtifact) {
                    var isArtifact = child.GetComponent<Artifact>() != null;
                    if (isArtifact) {
                        offset = Vector3.up * (3 / 4f);
                    }
                }

                child.transform.localPosition = Vector3.Lerp(child.transform.localPosition, offset, snapLerpSpeed * Time.deltaTime);
                child.transform.localRotation = Quaternion.Slerp(child.transform.localRotation, Quaternion.identity, snapSlerpSpeed * Time.deltaTime);
            }
        }
    }

    public bool CanSnap(IPlayerHoldable thing) {
        if (!allowSnap)
            return false;

        if (!isActiveAndEnabled) {
            return false;
        }
        
        switch (myAllowedSnaps) {
            case AllowedSnaps.nothing:
                return false;
            case AllowedSnaps.cart:
                return thing is Cart;
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
            case AllowedSnaps.scrapable:
                var scrapable = ((MonoBehaviour)thing).GetComponent<Scrapable>();
                return scrapable != null;
            case AllowedSnaps.red: {
                var scrapItem = ((MonoBehaviour)thing).GetComponent<ScrapsItem>();
                if (!scrapItem)
                    return false;
                return scrapItem.myType == ScrapsItem.ScrapsType.red;
            }
            case AllowedSnaps.blue: {
                var scrapItem = ((MonoBehaviour)thing).GetComponent<ScrapsItem>();
                if (!scrapItem)
                    return false;
                return scrapItem.myType == ScrapsItem.ScrapsType.blue;
            }
            case AllowedSnaps.green: {
                var scrapItem = ((MonoBehaviour)thing).GetComponent<ScrapsItem>();
                if (!scrapItem)
                    return false;
                return scrapItem.myType == ScrapsItem.ScrapsType.green;
            }
            case AllowedSnaps.anyColor: {
                var scrapItem = ((MonoBehaviour)thing).GetComponent<ScrapsItem>();
                if (!scrapItem)
                    return false;
                return true;
            }
            case AllowedSnaps.justComponentArtifact: {
                var artifact = thing as Artifact;
                if (!artifact) {
                    return false;
                } else {
                    return artifact.isComponent;
                }
            }
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

    public void UnSnapExistingObject() {
        var snappedObject = GetSnappedObject();
        if (snappedObject != null) {
            snappedObject.transform.SetParent(null);
            snappedObject.GetComponent<Rigidbody>().isKinematic = false;
            snappedObject.GetComponent<Rigidbody>().useGravity = true;
            snappedObject.GetComponent<Rigidbody>().AddForce(SmitheryController.GetRandomYeetForce());

            if (snappedObject.GetComponent<Artifact>() != null) {
                ShopStateController.s.AddArtifactToShop(snappedObject.GetComponent<Artifact>());
            } else if (snappedObject.GetComponent<Cart>() != null) {
                ShopStateController.s.AddCartToShop(snappedObject.GetComponent<Cart>());
            }
        }
    }

    public void SetVisualizeState(bool state) {
        if (visualizeEffect != null) {
            visualizeEffect.SetActive(state);
        }
    }
}
