using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class SmitheryController : MonoBehaviour
{
    
    public SnapLocation location1;
    public SnapLocation location2;

    public GameObject allParent;

    public Mini_Smithery smithery;
    
    [ValueDropdown("GetAllModuleNames")]
    public string scrapCart = "";

    private void Start() {
        smithery.OnStuffCollided.AddListener(UpgradeDone);
    }

    void Update()
    {
        if (!isEngaged && !PlayerWorldInteractionController.s.isDragging() && PlayerWorldInteractionController.s.canSmith) {
            if (!location1.IsEmpty() && !location2.IsEmpty())
                CheckAndDoUpgrade();
        }

        if (PlayerWorldInteractionController.s.canSmith) {
            location1.myAllowedSnaps = SnapLocation.AllowedSnaps.nothing;
            location2.myAllowedSnaps = SnapLocation.AllowedSnaps.nothing;
        } else {
            location1.myAllowedSnaps = SnapLocation.AllowedSnaps.cart;
            location2.myAllowedSnaps = SnapLocation.AllowedSnaps.cart;
        }
    }


    private bool isCartUpgrade = false;
    void CheckAndDoUpgrade() {
        var cart1 = location1.GetComponentInChildren<Cart>();
        var cart2 = location2.GetComponentInChildren<Cart>();
        var artifact1 = location1.GetComponentInChildren<Artifact>();
        var artifact2 = location2.GetComponentInChildren<Artifact>();

        /*if (cart1 != null && cart1.uniqueName == scrapCart) {
            if (cart2 != null) {
                if (cart2.level < 2) {
                    EngageUpgrade(true);
                    return;
                }
            }else if (artifact2 != null) {
                if (artifact2.level < 2) {
                    EngageUpgrade(false);
                    return;
                }
            }

        }else if (cart2 != null && cart2.uniqueName == scrapCart) {
            if (cart1 != null) {
                if (cart1.level < 2) {
                    EngageUpgrade(true);
                    return;
                }
            }else if (artifact1 != null) {
                if (artifact1.level < 2) {
                    EngageUpgrade(false);
                    return;
                }
            }
        }

        if (cart1 != null && cart2 != null) {
            if (cart1.level < 2 && cart1.level == cart2.level && cart1.uniqueName == cart2.uniqueName) {
                EngageUpgrade(true);
                return;
            }
        }
        
        
        
        if (artifact1 != null && artifact2 != null) {
            if (artifact1.level < 2 && artifact1.level == artifact2.level && artifact1.uniqueName == artifact2.uniqueName) {
                EngageUpgrade(false);
                return;
            }
        }*/
    }
    
    
    
    public float rotateSpeed = 20;
    public float rotateAcceleration = 20;

    private bool isEngaged = false;
    
    void EngageUpgrade(bool _isCartUpgrade) {
        isCartUpgrade = _isCartUpgrade;
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

        /*if (isCartUpgrade) {
            if (cart1.uniqueName == scrapCart) {
                UpgradesController.s.RemoveCartFromShop(cart1);
                Destroy(cart1.gameObject);
                cart2.level += 1;
            } else {
                UpgradesController.s.RemoveCartFromShop(cart2);
                Destroy(cart2.gameObject);
                cart1.level += 1;
            }
        } else {
            var artifact1 = location1.GetComponentInChildren<Artifact>();
            var artifact2 = location2.GetComponentInChildren<Artifact>();

            if (cart1 != null) {
                UpgradesController.s.RemoveCartFromShop(cart1);
                Destroy(cart1.gameObject);
                artifact2.level += 1;
            } else if (cart2 != null) {
                UpgradesController.s.RemoveCartFromShop(cart2);
                Destroy(cart2.gameObject);
                artifact1.level += 1;
            } else {
                UpgradesController.s.RemoveArtifactFromShop(artifact1);
                Destroy(artifact1.gameObject);
                artifact2.level += 1;
            }
        }*/


        Train.s.CartOrArtifactUpgraded();
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
