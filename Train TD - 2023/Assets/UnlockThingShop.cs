using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnlockThingShop : MonoBehaviour {

    public UnlockThingsSlot[] slots;
    void Start()
    {
        GenerateShop();
    }

    void GenerateShop() {
        var allUnlockableArtifacts = new List<Artifact>();
        var allUnlockableCarts = new List<Cart>();

        var allAffordableArtifacts = new List<Artifact>();
        var allAffordableCarts = new List<Cart>();

        var unlockedThings = DataSaver.s.GetCurrentSave().metaProgress.unlockedThings;
        var allArtifacts = DataHolder.s.artifacts;
        var allCarts = DataHolder.s.buildings;
        var currentMoney = DataSaver.s.GetCurrentSave().metaProgress.money;

        for (int i = 0; i < allArtifacts.Length; i++) {
            if (allArtifacts[i].needsToBeBought) {
                if (!unlockedThings.Contains(allArtifacts[i].uniqueName)) {
                    allUnlockableArtifacts.Add(allArtifacts[i]);
                    if (allArtifacts[i].buyCost <= currentMoney) {
                        allAffordableArtifacts.Add(allArtifacts[i]);
                    }
                }
            }
        }
        
        for (int i = 0; i < allCarts.Length; i++) {
            if (allCarts[i].needsToBeBought) {
                if (!unlockedThings.Contains(allCarts[i].uniqueName)) {
                    allUnlockableCarts.Add(allCarts[i]);
                    if (allCarts[i].buyCost <= currentMoney) {
                        allAffordableCarts.Add(allCarts[i]);
                    }
                }
            }
        }

        for (int i = 0; i < slots.Length; i++) {
            slots[i].Clear();
        }
        

        // first slot will always be an artifact you can afford
        if (allAffordableArtifacts.Count > 0) {
            var insertedThing = allAffordableArtifacts[Random.Range(0, allAffordableArtifacts.Count)];
            allUnlockableArtifacts.Remove(insertedThing);
            slots[0].SetUp(insertedThing);
        }

        // second slot will always be a cart you can afford
        if (allAffordableCarts.Count > 0) {
            var insertedThing = allAffordableCarts[Random.Range(0, allAffordableCarts.Count)];
            allUnlockableCarts.Remove(insertedThing);
            slots[1].SetUp(insertedThing);
        }

        // fill the remaining slots randomly
        for (int i = 0; i < slots.Length; i++) {
            if(!slots[i].isEmpty)
                continue;
            
            if (allUnlockableArtifacts.Count <= 0 && allUnlockableCarts.Count <= 0) {
                slots[i].TellNoMoreToUnlock();
                continue;
            }
            
            
            bool doArtifact;
            if (allUnlockableArtifacts.Count <= 0) {
                doArtifact = false;
            }else if (allUnlockableCarts.Count <= 0) {
                doArtifact = true;
            } else {
                doArtifact = Random.value > 0.5f;
            }

            if (doArtifact) {
                var insertedThing = allUnlockableArtifacts[Random.Range(0, allUnlockableArtifacts.Count)];
                allUnlockableArtifacts.Remove(insertedThing);
                slots[i].SetUp(insertedThing);
            } else {
                var insertedThing = allUnlockableCarts[Random.Range(0, allUnlockableCarts.Count)];
                allUnlockableCarts.Remove(insertedThing);
                slots[i].SetUp(insertedThing);
            }
        }
    }

    public void Sold() {
        for (int i = 0; i < slots.Length; i++) {
            slots[i].CanNoLongerBuy();
        }
    }
}
