using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RoboRepairModule : MonoBehaviour, IActiveDuringCombat, IResetState, IExtraInfo {

    private float curDelay = 0.5f;
    public float delay = 2;
    public float amount = 25;

    public float amountMultiplier = 1f;
    public float amountDivider = 1f;
    public float firerateMultiplier = 1f;
    public float firerateDivider = 1f;
    //public float steamUsePerRepair = 0.5f;

    public bool explosiveRepair = false;
    public float explosiveRepairAmount = 0;

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
        return amount * amountMultiplier * (1/amountDivider);
    }

    float GetDelay() {
        return delay * (1 / firerateMultiplier) * firerateDivider;
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
                var canRepair = (healths[i].currentHealth < healths[i].maxHealth && doImperfect) ||
                                healths[i].currentHealth <= healths[i].maxHealth - amount;

                if (canRepair) {
                    healths[i].Repair(amount);
                    if (explosiveRepair) {
                        Train.s.GetNextBuilding(1, healths[i].myCart)?.GetHealthModule().Repair(amount*explosiveRepairAmount);
                        Train.s.GetNextBuilding(-1, healths[i].myCart)?.GetHealthModule().Repair(amount*explosiveRepairAmount);
                    }
                    SpawnGemEffect(healths[i]);
                    return true;
                }
            }
        }

        return false;
    }

    void SpawnGemEffect(ModuleHealth target) {
        StartCoroutine(_SpawnGemEffect(target));
    }

    IEnumerator _SpawnGemEffect(ModuleHealth target) {
        foreach (var prefab in extraPrefabToSpawnOnAffected) {
            if (target == null) {
                yield break;
            }
            
            Instantiate(prefab, target.GetUITransform());

            yield return new WaitForSeconds(0.2f);
        }
    }

    public void ActivateForCombat() {
        myTrain = GetComponentInParent<Train>();
        myCart = GetComponent<Cart>();
        this.enabled = true;
    }

    public void Disable() {
        this.enabled = false;
    }


    public void ResetState() {
        extraPrefabToSpawnOnAffected.Clear();
        firerateMultiplier = 1;
        amountMultiplier = 1;
        firerateDivider = 1;
        explosiveRepair = false;
        explosiveRepairAmount = 0;
        amountDivider = 1;
    }
}
