using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class ScrapPaymentSlot : MonoBehaviour {

    public List<ScrapsItem.ScrapsType> requiredTypes = new List<ScrapsItem.ScrapsType>();
    public List<SnapLocation> allSnaps = new List<SnapLocation>();

    public GameObject redSnap;
    public GameObject blueSnap;
    public GameObject greenSnap;
    public GameObject universalSnap;
    
    public float snapHeight = 0.4f;

    public float goUpSpeed = 20;
    public float goDownSpeed = 40;

    public bool isMoving = false;
    public bool isOpen = false;

    public Transform snapParent;

    private void Start() {
        isOpen = false;
        SetScrapLocations();
        Invoke(nameof(DoOpen), Random.Range(0.2f,0.5f));
    }

    public void SetScrapLocations() {
        allSnaps.Clear();
        snapParent.DeleteAllChildren();

        var spawnPos = Vector3.zero;

        for (int i = 0; i < requiredTypes.Count; i++) {
            var prefab = GetPrefabBasedOnType(requiredTypes[i]);
            var scrapSlot = Instantiate(prefab, snapParent);
            scrapSlot.transform.localRotation = Quaternion.identity;
            scrapSlot.transform.localPosition = spawnPos;
            spawnPos.x -= snapHeight;
            allSnaps.Add(scrapSlot.GetComponent<SnapLocation>());
        }
        
        snapParent.transform.rotation = Quaternion.Euler(-180,0,0);
    }

    
    [Button]
    void SetScrapLocationsEditor() {
        allSnaps.Clear();
        snapParent.DeleteAllChildrenEditor();

        var spawnPos = Vector3.zero;

        for (int i = 0; i < requiredTypes.Count; i++) {
            var prefab = GetPrefabBasedOnType(requiredTypes[i]);
            var scrapSlot = Instantiate(prefab, snapParent);
            scrapSlot.transform.localRotation = Quaternion.identity;
            scrapSlot.transform.localPosition = spawnPos;
            spawnPos.x -= snapHeight;
            allSnaps.Add(scrapSlot.GetComponent<SnapLocation>());
        }
        
        snapParent.transform.localRotation = Quaternion.Euler(0,0,0);
    }

    public void DoPayment() {
        SetOpenState(false);
    }


    public bool AllSlotsFull() {
        for (int i = 0; i < allSnaps.Count; i++) {
            if (allSnaps[i].IsEmpty()) {
                return false;
            }
        }

        return true;
    }
    
    
    

    void SetSnappableState() {
        var snapLocations = snapParent.GetComponentsInChildren<SnapLocation>();

        var snappable = !isMoving && isOpen;

        for (int i = 0; i < snapLocations.Length; i++) {
            snapLocations[i].allowSnap = snappable;
            var snappedScrap = snapLocations[i].GetSnappedObject();
            /*if (snappedScrap != null) {
                snappedScrap.GetComponent<ScrapsItem>().canHold = snappable;
            }*/
        }
    }

    void DoOpen() {
        SetOpenState(true);
    }
    void SetOpenState(bool open) {
        isOpen = open;
        isMoving = true;
        SetSnappableState();
    }


    private void Update() {
        if (isMoving) {
            var targetRotation = isOpen ? Quaternion.identity : Quaternion.Euler(180, 0, 0);
            var moveSpeed = isOpen ? goUpSpeed : goDownSpeed;

            snapParent.transform.localRotation = Quaternion.RotateTowards(snapParent.transform.localRotation, targetRotation, moveSpeed * Time.deltaTime);

            if (snapParent.transform.localRotation == targetRotation) {
                isMoving = false;
                SetSnappableState();
            }
        } else {
            if (!isOpen) {
                for (int i = 0; i < allSnaps.Count; i++) {
                    if (allSnaps[i].GetSnappedObject() != null) {
                        if (allSnaps[i].GetSnappedObject().GetComponent<Artifact>()) {
                            ShopStateController.s.RemoveArtifactFromShop(allSnaps[i].GetSnappedObject().GetComponent<Artifact>());
                        }else if (allSnaps[i].GetSnappedObject().GetComponent<Cart>()) {
                            ShopStateController.s.RemoveCartFromShop(allSnaps[i].GetSnappedObject().GetComponent<Cart>());
                        }
                        
                        Destroy(allSnaps[i].GetSnappedObject().gameObject);
                    }
                }
                SetOpenState(true);
            }
        }
    }

    GameObject GetPrefabBasedOnType(ScrapsItem.ScrapsType type) {
        switch (type) {
            case ScrapsItem.ScrapsType.red:
                return redSnap;
            case ScrapsItem.ScrapsType.blue:
                return blueSnap;
            case ScrapsItem.ScrapsType.green:
                return greenSnap;
            case ScrapsItem.ScrapsType.colorless:
                return universalSnap;
            default:
                return null;
        }
    }
}
