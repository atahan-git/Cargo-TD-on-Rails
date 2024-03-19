using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CrystalsAndWarpController : MonoBehaviour {
    public static CrystalsAndWarpController s;

    public Transform coinGoPosition;
    public TMP_Text coinText;

    public TMP_Text warpNeedsText;
    public Button warpButton;

    public int crystalCount;
    public int maxCrystals;

    public bool warping = false;

    private void Awake() {
        s = this;
    }

    private void Start() {
        Train.s.onTrainCartsOrHealthOrArtifactsChanged.AddListener(CalculateTotalCrystalStorageAmount);
    }

    public void OnCombatStart() {
        crystalCount = 25;
        CalculateTotalCrystalStorageAmount();
    }


    public void GetCrystal(int count) {
        if (crystalCount < maxCrystals) {
            crystalCount += count;
            crystalCount = Mathf.Clamp(crystalCount, 0, maxCrystals);
        }
        
        CrystalCountsUpdated();
    }

    public int TryUseCrystals(int amount) {
        var usedAmount = Mathf.Min(amount, crystalCount);

        crystalCount -= usedAmount;

        CrystalCountsUpdated();
        return usedAmount;
    }

    void CrystalCountsUpdated() {
        coinText.text = $"x{crystalCount}/{maxCrystals}";

        if (!warping) {
            int cartCount = Train.s.carts.Count;
            int crystalNeed = cartCount * 25 + 25;
            int timeNeed = cartCount * 15 + 15;
            
            warpNeedsText.text = $"need {crystalNeed} - {ExtensionMethods.FormatTime(timeNeed)}";
            warpButton.interactable = crystalNeed <= crystalCount;
        }
    }


    public void EngageWarp() {
        
    }

    public void CalculateTotalCrystalStorageAmount() {
        maxCrystals = 50;

        for (int i = 0; i < Train.s.carts.Count; i++) {
            var cart = Train.s.carts[i];
            if (!cart.isDestroyed) {
                var storageModule = Train.s.carts[i].GetComponentInChildren<CrystalStorageModule>();
                if(storageModule)
                    maxCrystals += storageModule.amount;
            }
        }
        
        CrystalCountsUpdated();
    }

}
