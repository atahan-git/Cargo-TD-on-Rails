using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmitheryController : MonoBehaviour
{
    
    public SnapCartLocation location1;
    public SnapCartLocation location2;

    public GameObject allParent;

    public Mini_Smithery smithery;

    private void Start() {
        smithery.OnStuffCollided.AddListener(UpgradeDone);
    }

    void Update()
    {
        if (!isEngaged && !PlayerWorldInteractionController.s.isHoldDragStarted && PlayerWorldInteractionController.s.canSmith) {
            if (location1.snapTransform.childCount > 0 && location2.snapTransform.childCount > 0)
                CheckAndDoUpgrade();
        }

        if (PlayerWorldInteractionController.s.canSmith) {
            location1.snapNothing = false;
            location2.snapNothing = false;
        } else {
            location1.snapNothing = true;
            location2.snapNothing = true;
        }
    }


    void CheckAndDoUpgrade() {
        var cart1 = location1.GetComponentInChildren<Cart>();
        var cart2 = location2.GetComponentInChildren<Cart>();

        if (cart1.level < 2 && cart1.level == cart2.level && cart1.uniqueName == cart2.uniqueName) {
            EngageUpgrade();
        }
    }
    
    
    
     public float rotateSpeed = 20;
    public float rotateAcceleration = 20;

    private bool isEngaged = false;
    
    void EngageUpgrade() {
        isEngaged = true;
        
        PlayerWorldInteractionController.s.Deselect();
        SetColliderStatus(allParent, false);
        smithery.EngageAnim();
    }

    void UpgradeDone() {
        SetColliderStatus(allParent, true);
        isEngaged = false;
        
        var cart1 = location1.GetComponentInChildren<Cart>();
        var cart2 = location2.GetComponentInChildren<Cart>();
        
        UpgradesController.s.RemoveCartFromShop(cart1);
        Destroy(cart1.gameObject);
        cart2.level += 1;
            
        Train.s.CartUpgraded();
    }

    void SetColliderStatus(GameObject target, bool status) {
        var allColliders = target.GetComponentsInChildren<Collider>();
        for (int i = 0; i < allColliders.Length; i++) {
            allColliders[i].enabled = status;
        }
    }
    
}
