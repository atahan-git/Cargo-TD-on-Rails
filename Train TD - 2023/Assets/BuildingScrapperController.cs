using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class BuildingScrapperController : MonoBehaviour
{
    [ValueDropdown("GetAllModuleNames")]
    public string scrapCart = "";

    public SnapLocation target;

    public GameObject poofEffect;
    
    private bool isEngaged = false;
    // Update is called once per frame
    void Update()
    {
        if (!isEngaged && !PlayerWorldInteractionController.s.isDragging()) {
            if (!target.IsEmpty())
                CheckAndDoUpgrade();
        }
    }

    void CheckAndDoUpgrade() {
        bool didUpgrade = false;
        var cart = target.GetComponentInChildren<Cart>();

        if (cart != null) {
            if (cart.uniqueName != scrapCart) {
                ShopStateController.s.RemoveCartFromShop(cart);
                Destroy(cart.gameObject);
                Instantiate(poofEffect, target.transform);
                Instantiate(DataHolder.s.GetCart(scrapCart), target.transform);
                didUpgrade = true;
            }
        }


        var artifact = target.GetComponentInChildren<Artifact>();

        if (artifact != null) {
            ShopStateController.s.RemoveArtifactFromShop(artifact);
            Destroy(artifact.gameObject);
            Instantiate(poofEffect, target.transform);
            Instantiate(DataHolder.s.GetCart(scrapCart), target.transform);
            didUpgrade = true;
        }

        if (didUpgrade) {
            Train.s.TrainChanged();
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
