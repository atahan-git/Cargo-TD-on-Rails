using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RoboRepairModule : ActivateWhenAttachedToTrain, IActiveDuringCombat, IBooster, IResetState, IExtraInfo {

    public enum RoboType {
        repair, reload, shields
    }

    public RoboType myType = RoboType.repair;
    
    private float curDelay = 0.5f;
    public float delay = 2;
    public float amount = 25;
    //public float steamUsePerRepair = 0.5f;

    public int countPerCycle = 2;

    public bool hasAmmo = true;
    
    private Train myTrain;
    private Cart myCart;
    private ModuleAmmo _ammo;

    [HideInInspector]
    public UnityEvent OnRepaired = new UnityEvent();
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
            if (curDelay <= 0 && !myCart.isDestroyed && hasAmmo) {
                /*if(BreadthFirstRepairSearch())
                    SpeedController.s.UseSteam(steamUsePerRepair);*/
                if (BreadthFirstRepairSearch()) {
                    OnRepaired.Invoke();
                }
                curDelay = delay;
            } else {
                curDelay -= Time.deltaTime;
            }
        }
    }

    public string GetInfoText() {
        return $"Repairs {amount*2} per {curDelay:F1} seconds {GetRange()} nearby carts";
    }
    public void InstantRepair(bool doRepair) {
        if (doRepair) {
            BreadthFirstRepairSearch();
            BreadthFirstRepairSearch();
        }
    }


    bool BreadthFirstRepairSearch() {
        var repairCount = 0;
        
        // always repair self first
        if (DoThingInCart(myCart, true)) {
            repairCount += 1;
        }
        
        
        for (int i = 1; i < GetRange()+1; i++) {
            if (DoThingInCart(Train.s.GetNextBuilding(i, myCart), false)) {
                repairCount += 1;
            }
            if (repairCount >= countPerCycle) {
                return true;
            }
            if (DoThingInCart(Train.s.GetNextBuilding(-i, myCart), false)) {
                repairCount += 1;
            }
            if (repairCount >= countPerCycle) {
                return true;
            }
        }

        
        
        for (int i = 1; i < GetRange()+1; i++) {
            if (DoThingInCart(Train.s.GetNextBuilding(i, myCart), true)) {
                repairCount += 1;
            }
            if (repairCount >= countPerCycle) {
                return true;
            }
            if (DoThingInCart(Train.s.GetNextBuilding(-i, myCart), true)) {
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
    
    bool DoThingInCart(Cart target, bool doImperfect) {
        if (target == null) {
            return false;
        } 
        var healths = target.GetComponentsInChildren<ModuleHealth>();
        switch (myType) {
            case RoboType.repair:
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
                break;
            case RoboType.shields:
                if (healths.Length > 0) {
                    for (int i = 0; i < healths.Length; i++) {
                        var canShield = healths[i].maxShields > 0 && 
                                        (
                                            (healths[i].currentShields < healths[i].maxShields && doImperfect) ||
                                        healths[i].currentShields <= healths[i].maxShields - amount
                                            );

                        if (canShield) {
                            healths[i].ShieldUp(amount);
                            return true;
                        }
                    }
                }
                break;
            case RoboType.reload:
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
                break;
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
        var health = target.GetComponentInChildren<ModuleHealth>();
        switch (myType) {
            case RoboType.repair:
                return health != null;
                break;
            case RoboType.shields:
                return health.maxShields > 0;
                break;
            case RoboType.reload:
                var ammo = target.GetComponentInChildren<ModuleAmmo>();
                return ammo != null;
                break;
        }

        return false;
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
