using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CrystalsController : MonoBehaviour {
    public static CrystalsController s;

    public Transform coinGoPosition;
    public TMP_Text coinText;


    public int crystalCount;
    public int maxCrystals;

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
    }

    public int TryUseCrystals(int amount) {
        var usedAmount = Mathf.Min(amount, crystalCount);

        crystalCount -= usedAmount;

        return usedAmount;
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
    }

}
