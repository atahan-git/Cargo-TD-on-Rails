using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RoboRepairModule : MonoBehaviour, IActiveDuringCombat, IResetState, IDisabledState {

    private float curDelay = 0.5f;
    public float delay = 2;
    public float amount = 25;

    private int countPerCycle = 1;

    public bool hasAmmo = true;
    
    private Train myTrain;
    private Cart myCart;
    private ModuleAmmo _ammo;

    [HideInInspector]
    public UnityEvent OnRepaired = new UnityEvent();

    public List<GameObject> extraPrefabToSpawnOnAffected = new List<GameObject>();
    private void Start() {
        myTrain = GetComponentInParent<Train>();
        myCart = GetComponentInParent<Cart>();
        
        if (myTrain == null)
            this.enabled = false;
    }

    float GetAmount() {
        return amount;
    }

    float GetDelay() {
        return delay;
    }
    
    void Update() {
        if (myTrain == null || myCart == null) {
            myTrain = GetComponentInParent<Train>();
            myCart = GetComponentInParent<Cart>();
            if (myTrain == null || myCart == null) {
                this.enabled = false;
            }
        }

        if (PlayStateMaster.s.isCombatInProgress()) {
            if (curDelay <= 0 && !myCart.isDestroyed && hasAmmo) {
                /*if(BreadthFirstRepairSearch())
                    SpeedController.s.UseSteam(steamUsePerRepair);*/
                if (BreadthFirstRepairSearch(GetAmount())) {
                    OnRepaired.Invoke();
                }
                curDelay = GetDelay();
            } else {
                curDelay -= Time.deltaTime;
            }
        }
    }

    public string GetInfoText() {
        return $"Repairs {GetAmount()*countPerCycle} per {GetDelay():F1} seconds";
    }

    private int halfRepairCount = 0;
    public void InstantRepair(bool fullRepair) {
        if (!myCart.isDestroyed) {
            if (fullRepair) {
                BreadthFirstRepairSearch(GetAmount() / 2f);
            } else {
                if (halfRepairCount <= 0) {
                    BreadthFirstRepairSearch(GetAmount() / 2f);
                    halfRepairCount = 5;
                } else {
                    halfRepairCount -= 1;
                }
            }
        }
    }


    bool BreadthFirstRepairSearch(float amount) {
        var repairCount = 0;
        
        // always repair self first
        if (DoThingInCart(myCart, true, amount)) {
            repairCount += 1;
        }
        
        if (repairCount >= countPerCycle) {
            return true;
        }

        var range = Train.s.carts.Count;
        
        for (int i = 1; i < range+1; i++) {
            if (DoThingInCart(Train.s.GetNextBuilding(i, myCart), false, amount)) {
                repairCount += 1;
            }
            if (repairCount >= countPerCycle) {
                return true;
            }
            if (DoThingInCart(Train.s.GetNextBuilding(-i, myCart), false, amount)) {
                repairCount += 1;
            }
            if (repairCount >= countPerCycle) {
                return true;
            }
        }

        
        
        for (int i = 1; i < range+1; i++) {
            if (DoThingInCart(Train.s.GetNextBuilding(i, myCart), true, amount)) {
                repairCount += 1;
            }
            if (repairCount >= countPerCycle) {
                return true;
            }
            if (DoThingInCart(Train.s.GetNextBuilding(-i, myCart), true, amount)) {
                repairCount += 1;
            }
            if (repairCount >= countPerCycle) {
                return true;
            }
        }

        if (repairCount > 0) {
            return true;
        }

        return false;
    }
    
    bool DoThingInCart(Cart target, bool doImperfect, float amount) {
        if (target == null) {
            return false;
        } 
        var healths = target.GetComponentsInChildren<ModuleHealth>();
        if (healths.Length > 0) {
            for (int i = 0; i < healths.Length; i++) {
                var canRepair = (healths[i].currentHealth < healths[i].GetMaxHealth() && doImperfect) ||
                                healths[i].currentHealth <= healths[i].GetMaxHealth() - amount;

                if (canRepair) {
                    healths[i].RepairChunk();
                    return true;
                }
            }
        }

        return false;
    }

    public void ActivateForCombat() {
        myTrain = GetComponentInParent<Train>();
        myCart = GetComponent<Cart>();
        
        this.enabled = true;
    }

    public void Disable() {
        this.enabled = false;
    }
    
    public void CartDisabled() {
        this.enabled = false;
    }

    public void CartEnabled() {
        this.enabled = true;
    }


    public void ResetState() {
        
    }
}
