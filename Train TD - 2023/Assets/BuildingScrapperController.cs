using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class BuildingScrapperController : MonoBehaviour
{
    [ValueDropdown("GetAllModuleNames")]
    public string scrapCart = "";

    public SnapCartLocation target;

    public GameObject poofEffect;
    
    private bool isEngaged = false;
    // Update is called once per frame
    void Update()
    {
        if (!isEngaged && !PlayerWorldInteractionController.s.isDragging() && PlayerWorldInteractionController.s.canSmith) {
            if (target.snapTransform.childCount > 0)
                CheckAndDoUpgrade();
        }
    }

    void CheckAndDoUpgrade() {
        bool didUpgrade = false;
        var cart = target.GetComponentInChildren<Cart>();

        if (cart != null) {
            if (cart.uniqueName != scrapCart) {
                UpgradesController.s.RemoveCartFromShop(cart);
                Destroy(cart.gameObject);
                Instantiate(poofEffect, target.snapTransform);
                Instantiate(DataHolder.s.GetCart(scrapCart), target.snapTransform);
                didUpgrade = true;
            }
        }


        var artifact = target.GetComponentInChildren<Artifact>();

        if (artifact != null) {
            UpgradesController.s.RemoveArtifactFromShop(artifact);
            Destroy(artifact.gameObject);
            Instantiate(poofEffect, target.snapTransform);
            Instantiate(DataHolder.s.GetCart(scrapCart), target.snapTransform);
            didUpgrade = true;
        }

        if (didUpgrade) {
            Train.s.CartOrArtifactUpgraded();
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
