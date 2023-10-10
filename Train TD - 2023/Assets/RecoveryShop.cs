using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecoveryShop : MonoBehaviour {

    public enum UpgradeType {
        armor, damage, ammo
    }

    private int myLevel = 0;
    private int curCost = 99999;
    public int[] costs = new int[3];

    private MoneyUIDisplay _moneyUIDisplay;
    private StarterShopButton _starterShopButton;

    public Tooltip maxLevelTooltip;

    public SnapCartLocation gemPlatform;
    public SnapCartLocation componentPlatform;
    public SnapCartLocation cartPlatform;
    
    void Start() {
        _moneyUIDisplay = GetComponentInChildren<MoneyUIDisplay>();
        _starterShopButton = GetComponent<StarterShopButton>();
        var metaProgress = DataSaver.s.GetCurrentSave().metaProgress;
        myLevel = metaProgress.recoveryUpgradesBought;
        
        _starterShopButton.OnPress.AddListener(TryPurchase);
        UpdateStatsBasedOnLevel();
    }

    private void UpdateStatsBasedOnLevel() {
        if (myLevel < costs.Length) {
            curCost = costs[myLevel];
            _moneyUIDisplay.SetAmount(costs[myLevel]);
        } else {
            curCost = int.MaxValue;
            _moneyUIDisplay.SetAmount(0);
            _starterShopButton.myTooltip = maxLevelTooltip;
            _starterShopButton.SetStatus(false);
        }
        
        gemPlatform.gameObject.SetActive(false);
        componentPlatform.gameObject.SetActive(false);
        cartPlatform.gameObject.SetActive(false);

        var metaProgress = DataSaver.s.GetCurrentSave().metaProgress;
        
        if (myLevel >= 1) {
            gemPlatform.gameObject.SetActive(true);
            if (metaProgress.bonusGem != null && metaProgress.bonusGem.Length > 0) {
                Instantiate(DataHolder.s.GetArtifact(metaProgress.bonusGem), gemPlatform.snapTransform);
            }
        }
        
        if (myLevel >= 2) {
            componentPlatform.gameObject.SetActive(true);
            if (metaProgress.bonusComponent != null && metaProgress.bonusComponent.Length > 0) {
                Instantiate(DataHolder.s.GetArtifact(metaProgress.bonusComponent), componentPlatform.snapTransform);
            }
        }
        
        if (myLevel >= 3) {
            cartPlatform.gameObject.SetActive(true);
            if (metaProgress.bonusCart != null && metaProgress.bonusCart.Length > 0) {
                Instantiate(DataHolder.s.GetCart(metaProgress.bonusCart), cartPlatform.snapTransform);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(myLevel < costs.Length)
            _starterShopButton.SetStatus(DataSaver.s.GetCurrentSave().metaProgress.money >= curCost);
    }

    public void TryPurchase() {
        if (myLevel < costs.Length) {
            if (curCost <= DataSaver.s.GetCurrentSave().metaProgress.money) {
                DataSaver.s.GetCurrentSave().metaProgress.money -= curCost;
                myLevel += 1;
                var metaProgress = DataSaver.s.GetCurrentSave().metaProgress;
                metaProgress.recoveryUpgradesBought = myLevel;
                UpdateStatsBasedOnLevel();
                DataSaver.s.SaveActiveGame();
            }
        }
    }
}
