using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicUpgrade : MonoBehaviour
{
    public enum UpgradeType {
        armor, damage, ammo
    }

    private int myLevel = 0;
    private int curCost = 99999;
    public UpgradeType myType;
    public int[] costs = new int[3];

    private MoneyUIDisplay _moneyUIDisplay;
    private StarterShopButton _starterShopButton;

    public Tooltip maxLevelTooltip;
    
    void Start() {
        _moneyUIDisplay = GetComponentInChildren<MoneyUIDisplay>();
        _starterShopButton = GetComponent<StarterShopButton>();
        var metaProgress = DataSaver.s.GetCurrentSave().metaProgress;
        switch (myType) {
            case UpgradeType.ammo:
                myLevel = metaProgress.ammoUpgradesBought;
                break;
            case UpgradeType.armor:
                myLevel = metaProgress.armorUpgradesBought;
                break;
            case UpgradeType.damage:
                myLevel = metaProgress.damageUpgradesBought;
                break;
        }
        
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
                switch (myType) {
                    case UpgradeType.ammo:
                        metaProgress.ammoUpgradesBought = myLevel;
                        break;
                    case UpgradeType.armor:
                        metaProgress.armorUpgradesBought = myLevel;
                        break;
                    case UpgradeType.damage:
                        metaProgress.damageUpgradesBought = myLevel;
                        break;
                }
                UpdateStatsBasedOnLevel();
                DataSaver.s.SaveActiveGame();
            }
        }
    }
}
