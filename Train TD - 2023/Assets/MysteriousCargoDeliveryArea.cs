using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class MysteriousCargoDeliveryArea : MonoBehaviour
{
   public SnapCartLocation deliverPlatform;
   public SnapCartLocation newCartPlatform;

    public Transform rotatingPlatform;

    public Transform[] artifactLocations;
    public Transform pickOne;

    [ValueDropdown("GetAllModuleNames")]
    public string act2MysteriousCart;
    [ValueDropdown("GetAllModuleNames")]
    public string act3MysteriousCart;

    public void Start() {
        PlayStateMaster.s.OnShopEntered.AddListener(ResetArea);
    }

    void ResetArea() {
        rotatingPlatform.transform.rotation = Quaternion.identity;
        pickOne.gameObject.SetActive(false);
        deliverPlatform.SetEmptyStatus(true);
    }

    public bool isSpawned = false;
    public bool rewardSpawned = false;
    void Update()
    {
        if (!isEngaged && !PlayerWorldInteractionController.s.isHoldDragStarted) {
            if (deliverPlatform.snapTransform.childCount > 0) 
                StartCoroutine(EngagePlatform());
        }

        if (isSpawned) {
            for (int i = 0; i < artifactLocations.Length; i++) {
                if (artifactLocations[i].childCount == 0) {
                    isSpawned = false;
                }
            }

            if (!isSpawned) {
                StartCoroutine(HideNonSelectedArtifacts());
                pickOne.gameObject.SetActive(false);
            }
        }

        if (newCartPlatform.snapTransform.childCount == 0 && rewardSpawned) {
            GetComponentInParent<Animator>().SetTrigger("UnEngage");
            rewardSpawned = false;
        }
    }

    public float rotateSpeed = 120;
    public float rotateAcceleration = 60;

    private bool isEngaged = false;
    
    IEnumerator EngagePlatform() {
        isEngaged = true;
        PlayerWorldInteractionController.s.Deselect();

        
        deliverPlatform.SetEmptyStatus(false);

        var bossArtifacts = UpgradesController.s.GetRandomBossArtifacts(artifactLocations.Length);
        for (int i = 0; i < artifactLocations.Length; i++) {
            Instantiate(DataHolder.s.GetArtifact(bossArtifacts[i]).gameObject, artifactLocations[i]).transform.position += Vector3.up*0.2f;
        }

        var act = DataSaver.s.GetCurrentSave().currentRun.currentAct;
        var rewardCart = Instantiate(DataHolder.s.GetCart(act == 1 ? act2MysteriousCart : act3MysteriousCart).gameObject, newCartPlatform.snapTransform);

        SetColliderStatus(rotatingPlatform.gameObject, false);

        var rotateTarget = (rotatingPlatform.rotation.eulerAngles.y > 25) ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);
        var totalDelta = Quaternion.Angle(rotatingPlatform.rotation, rotateTarget);
        var currentDelta = Quaternion.Angle(rotatingPlatform.rotation, rotateTarget);
        var curRotateSpeed = 0f;
        
        GetComponentInParent<Animator>().SetTrigger("Engage");

        while (currentDelta > 0.1f) {
            rotatingPlatform.rotation = Quaternion.RotateTowards(rotatingPlatform.rotation, rotateTarget, curRotateSpeed * Time.deltaTime);
            curRotateSpeed += rotateAcceleration * Time.deltaTime;
            curRotateSpeed = Mathf.Clamp(curRotateSpeed, 0, rotateSpeed);

            currentDelta = Quaternion.Angle(rotatingPlatform.rotation, rotateTarget); 
            yield return null;
        }

        rotatingPlatform.rotation = rotateTarget;
        
        SetColliderStatus(rotatingPlatform.gameObject, true);
        UpgradesController.s.AddCartToShop(rewardCart.GetComponent<Cart>(), UpgradesController.CartLocation.world);
        UpgradesController.s.RemoveCartFromShop(deliverPlatform.snapTransform.GetChild(0).GetComponent<Cart>());

        Destroy(deliverPlatform.snapTransform.GetChild(0).gameObject);

        yield return new WaitForSeconds(0f);
        
        SetColliderStatus(rotatingPlatform.gameObject, true);
        pickOne.gameObject.SetActive(true);

        MissionWinFinisher.s.needToDeliverMysteriousCargo = false;
        isSpawned = true;
        rewardSpawned = true;
        isEngaged = false;
    }
    
    IEnumerator HideNonSelectedArtifacts() {
        isEngaged = true;
        //PlayerWorldInteractionController.s.Deselect();

        SetColliderStatus(rotatingPlatform.gameObject, false);

        var rotateTarget = (rotatingPlatform.rotation.eulerAngles.y > 25) ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);
        var totalDelta = Quaternion.Angle(rotatingPlatform.rotation, rotateTarget);
        var currentDelta = Quaternion.Angle(rotatingPlatform.rotation, rotateTarget);
        var curRotateSpeed = 0f;

        while (currentDelta > 0.1f) {
            rotatingPlatform.rotation = Quaternion.RotateTowards(rotatingPlatform.rotation, rotateTarget, curRotateSpeed * Time.deltaTime);
            curRotateSpeed += rotateAcceleration * Time.deltaTime;
            curRotateSpeed = Mathf.Clamp(curRotateSpeed, 0, rotateSpeed);

            currentDelta = Quaternion.Angle(rotatingPlatform.rotation, rotateTarget); 
            yield return null;
        }

        rotatingPlatform.rotation = rotateTarget;

        for (int i = 0; i < artifactLocations.Length; i++) {
            artifactLocations[i].DeleteAllChildren();
        }

        yield return new WaitForSeconds(0.5f);
        
        SetColliderStatus(rotatingPlatform.gameObject, true);

        isEngaged = false;
    }

    void SetColliderStatus(GameObject target, bool status) {
        var allColliders = target.GetComponentsInChildren<Collider>();
        for (int i = 0; i < allColliders.Length; i++) {
            allColliders[i].enabled = status;
        }
    }
    
    
    private static IEnumerable GetAllModuleNames() {
        var buildings = GameObject.FindObjectOfType<DataHolder>().buildings;
        var buildingNames = new List<string>();
        buildingNames.Add("");
        for (int i = 0; i < buildings.Length; i++) {
            buildingNames.Add(buildings[i].uniqueName);
        }
        return buildingNames;
    }
}
