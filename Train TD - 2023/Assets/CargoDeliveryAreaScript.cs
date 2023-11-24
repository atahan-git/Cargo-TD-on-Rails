using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class CargoDeliveryAreaScript : MonoBehaviour {

    public SnapCartLocation location1;
    public SnapCartLocation location2;

    public Transform rotatingPlatform;

    public Transform artifactLocation1;
    public Transform artifactLocation2;

    public Transform extraArtifactEffect;

    public void Start() {
        PlayStateMaster.s.OnShopEntered.AddListener(ResetArea);
    }

    void ResetArea() {
        rotatingPlatform.transform.localRotation = Quaternion.identity;
        extraArtifactEffect.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isEngaged && !PlayerWorldInteractionController.s.isDragging()) {
            if (location1.snapTransform.childCount > 0) 
                StartCoroutine(EngagePlatform(location1, location2));
        }
    }

    public float rotateSpeed = 120;
    public float rotateAcceleration = 60;

    private bool isEngaged = false;

    IEnumerator EngagePlatform(SnapCartLocation fullPlatform, SnapCartLocation emptyPlatform) {
        isEngaged = true;
        PlayerWorldInteractionController.s.Deselect();
        SetColliderStatus(rotatingPlatform.gameObject, false);

        //yield return null; //wait a frame for good luck
        
        //give reward money
        var rewardMoney = 10 * DataSaver.s.GetCurrentSave().currentRun.currentAct;
        Instantiate(LevelReferences.s.coinDrop, LevelReferences.s.uiDisplayParent).GetComponent<CoinDrop>().SetUp(fullPlatform.transform.position, rewardMoney);

        var cargoModule = fullPlatform.GetComponentInChildren<CargoModule>();
        GameObject rewardCart = null;
        if (UpgradesController.s.rewardDestinationCart) {
            rewardCart = Instantiate(DataHolder.s.GetCart(cargoModule.GetState().cargoReward).gameObject, emptyPlatform.snapTransform);
        }

        GameObject rewardArtifact = null;
        if (UpgradesController.s.rewardDestinationArtifact) {
            rewardArtifact = Instantiate(DataHolder.s.GetArtifact(cargoModule.GetState().artifactReward).gameObject, artifactLocation1);
            rewardArtifact.transform.position += Vector3.up * 0.2f;
        }

        GameObject bonusRewardArtifact = null;
        var gotBonusArtifact = ArtifactsController.s.gotBonusArtifact;
        if (gotBonusArtifact) {
            bonusRewardArtifact = Instantiate(DataHolder.s.GetArtifact(ArtifactsController.s.bonusArtifactUniqueName).gameObject, artifactLocation2);
            bonusRewardArtifact.transform.position += Vector3.up*0.2f;
            ArtifactsController.s.BonusArtifactRewarded(artifactLocation2);
        }

        SetColliderStatus(rotatingPlatform.gameObject, false);

        var rotateTarget = (rotatingPlatform.localRotation.eulerAngles.y > 25) ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);
        var totalDelta = Quaternion.Angle(rotatingPlatform.localRotation, rotateTarget);
        var currentDelta = Quaternion.Angle(rotatingPlatform.localRotation, rotateTarget);
        var curRotateSpeed = 0f;

        while (currentDelta > 0.1f) {
            rotatingPlatform.localRotation = Quaternion.RotateTowards(rotatingPlatform.localRotation, rotateTarget, curRotateSpeed * Time.deltaTime);
            curRotateSpeed += rotateAcceleration * Time.deltaTime;
            curRotateSpeed = Mathf.Clamp(curRotateSpeed, 0, rotateSpeed);

            currentDelta = Quaternion.Angle(rotatingPlatform.localRotation, rotateTarget); 
            yield return null;
        }

        rotatingPlatform.localRotation = rotateTarget;
        
        SetColliderStatus(rotatingPlatform.gameObject, true);
        Destroy(fullPlatform.snapTransform.GetChild(0).gameObject);

        if (UpgradesController.s.rewardDestinationCart) {
            UpgradesController.s.AddCartToShop(rewardCart.GetComponent<Cart>(), UpgradesController.CartLocation.world);
        }

        UpgradesController.s.RemoveCartFromShop(fullPlatform.snapTransform.GetChild(0).GetComponent<Cart>());


        FirstTimeTutorialController.s.CargoHintShown();
        
        yield return new WaitForSeconds(0.5f);

        if (gotBonusArtifact) {
            extraArtifactEffect.gameObject.SetActive(true);
        }

        isEngaged = false;
    }
    
    Vector3 AddNoiseOnDirection (Vector3 direction, float max)  {
        // Generate a random rotation
        Quaternion randomRotation = Quaternion.Euler(Random.Range(-max, max), Random.Range(-max, max), 0);
        
        // Apply the random rotation to the current direction
        return randomRotation * direction;
    }

    void SetColliderStatus(GameObject target, bool status) {
        var allColliders = target.GetComponentsInChildren<Collider>();
        for (int i = 0; i < allColliders.Length; i++) {
            allColliders[i].enabled = status;
        }
    }
}
