using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoboRepairModule : ActivateWhenAttachedToTrain, IActiveDuringCombat, IBooster, IResetState {

    public bool isRepair = true;
    
    private float curDelay = 0.5f;
    public float delay = 2;
    public float amount = 25;
    //public float steamUsePerRepair = 0.5f;

    public int countPerCycle = 2;
    
    private Train myTrain;
    private Cart myCart;
    private void Start() {
        myTrain = GetComponentInParent<Train>();
        myCart = GetComponentInParent<Cart>();
        
        if (myTrain == null)
            this.enabled = false;
    }

    void Update() {
        myTrain = GetComponentInParent<Train>();
        myCart = GetComponentInParent<Cart>();
        
        if (myTrain == null || myCart == null)
            this.enabled = false;
        if (PlayStateMaster.s.isCombatInProgress()) {
            if (curDelay <= 0 && !myCart.isDestroyed) {
                /*if(BreadthFirstRepairSearch())
                    SpeedController.s.UseSteam(steamUsePerRepair);*/
                BreadthFirstRepairSearch();
                curDelay = delay;
            } else {
                curDelay -= Time.deltaTime;
            }
        }
    }


    bool BreadthFirstRepairSearch() {
        var carts = new List<Cart>();

        var range = GetRange();
        
        for (int i = 0; i < range; i++) {

            if (inBounds(myCart.trainIndex - i, myTrain.carts)) {
                var cart = myTrain.carts[myCart.trainIndex - i].GetComponent<Cart>();
                if(!carts.Contains(cart))
                    carts.Add(cart);
            }
            if (inBounds(myCart.trainIndex + i, myTrain.carts)) {
                var cart = myTrain.carts[myCart.trainIndex + i].GetComponent<Cart>();
                if(!carts.Contains(cart))
                    carts.Add(cart);
            }
        }

        var repairCount = 0;
        for (int i = 0; i < carts.Count; i++) {
            if (DoThingInCart(carts[i], false)) {
                repairCount += 1;
                if (repairCount >= countPerCycle) {
                    return true;
                }
            }
        }
        
        for (int i = 0; i < carts.Count; i++) {
            if (DoThingInCart(carts[i], true)) {
                if (repairCount >= countPerCycle) {
                    return true;
                }
            }
        }

        if (repairCount > 0) {
            return true;
        }

        return false;
    }
    
    private bool inBounds <T>(int index, List<T> array) 
    {
        return (index >= 0) && (index < array.Count);
    }

    bool DoThingInCart(Cart target, bool doImperfect) {

        if (isRepair) {
            var healths = target.GetComponentsInChildren<ModuleHealth>();

            if (healths.Length > 0) {
                for (int i = 0; i < healths.Length; i++) {
                    var canRepair = (healths[i].currentHealth < healths[i].maxHealth && doImperfect) ||
                                    healths[i].currentHealth <= healths[i].maxHealth - amount;

                    if (canRepair) {
                        healths[i].Repair(amount);
                        return true;
                    }
                }
            }
        } else {
            var ammos = target.GetComponentsInChildren<ModuleAmmo>();

            if (ammos.Length > 0) {
                for (int i = 0; i < ammos.Length; i++) {
                    var canReload = (ammos[i].curAmmo < ammos[i].maxAmmo && doImperfect) ||
                                    ammos[i].curAmmo <= ammos[i].maxAmmo - amount;

                    if (canReload) {
                        ammos[i].Reload(amount);
                        return true;
                    }
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
    
    
    protected override void _AttachedToTrain() {
        for (int i = 1; i < GetRange()+1; i++) {
            ApplyBoost(Train.s.GetNextBuilding(i, GetComponentInParent<Cart>()), true);
            ApplyBoost(Train.s.GetNextBuilding(-i, GetComponentInParent<Cart>()), true);
        }
    }

    protected override bool CanApply(Cart target) {
        if (isRepair) {
            var health = target.GetComponentInChildren<ModuleHealth>();
            return health != null;
        } else {
            var ammo = target.GetComponentInChildren<ModuleAmmo>();
            return ammo != null;
        }
    }

    protected override void _ApplyBoost(Cart target, bool doApply) {
        // do nothing
    }

    protected override void _DetachedFromTrain() {
        //do nothing
    }
    
    [Space]
    public int baseRange = 1;
    public int rangeBoost = 0;
    public float boostMultiplier = 1;

    public void ResetState(int level) {
        rangeBoost = level;
        boostMultiplier = 1;
    }

    public void ModifyStats(int range, float value) {
        rangeBoost += range;
        boostMultiplier += value;
    }

    public int GetRange() {
        return Mathf.Min(Train.s.carts.Count, baseRange + rangeBoost);
    }

    [ColorUsageAttribute(true, true)] public Color boostRangeColor = Color.green;
    public Color GetColor() {
        return boostRangeColor;
    }
}
