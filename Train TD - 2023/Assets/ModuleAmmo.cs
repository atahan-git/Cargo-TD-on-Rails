using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class ModuleAmmo : MonoBehaviour, IActiveDuringCombat, IActiveDuringShopping, IResetState, IAmmoProvider {

    [ShowInInspector]
    public float curAmmo { get; private set; }
    public int _maxAmmo = 100;
    public float maxAmmoMultiplier = 1f;

    [ShowInInspector]
    public int maxAmmo {
        get { return Mathf.RoundToInt(_maxAmmo * maxAmmoMultiplier); }
    }

    public PhysicalAmmoBar myAmmoBar;

    public void ResetState() {
        maxAmmoMultiplier = 1;
        reloadEfficiency = 1;

        maxAmmoMultiplier = 0.7f + (DataSaver.s.GetCurrentSave().cityUpgradesProgress.ammoUpgradesBought * 0.15f);

        myAmmoBar = GetComponentInChildren<PhysicalAmmoBar>();
        myAmmoBar.OnAmmoTypeChange();
        
        ChangeMaxAmmo(0);
        UpdateModuleState();
    }

    private void Start() {
        myAmmoBar = GetComponentInChildren<PhysicalAmmoBar>();
        myAmmoBar.OnAmmoTypeChange();
        UpdateModuleState();
    }

    public float AvailableAmmo() {
        return curAmmo;
    }

    public float AmmoCapacity() {
        return maxAmmo;
    }

    public void UseAmmo(float usedAmount) {
        curAmmo -= usedAmount;
        
        curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo*2);
        UpdateModuleState();
        myAmmoBar.OnUse(curAmmo);
    }



    public float reloadEfficiency = 1;

    [Button]
    public void Reload(float amount = -1, bool showEffect = true) {
        if (enabled) {
            amount *= reloadEfficiency;
            if (curAmmo < maxAmmo) {
                if (amount < 0) {
                    amount = maxAmmo;
                }

                if (showEffect)
                    Instantiate(LevelReferences.s.reloadEffect_regular, transform);

                curAmmo += amount;
                curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo*2);

                UpdateModuleState();
                
                if(myAmmoBar != null)
                    myAmmoBar.OnReload(showEffect, curAmmo);
            }
        } 
    }

    public void SetAmmo(int amount) {
        curAmmo = amount;
        curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo*2);

        UpdateModuleState();
        if(myAmmoBar != null)
            myAmmoBar.OnReload(false, curAmmo);
    }
    
    public void ChangeMaxAmmo(float multiplierChange) {
        maxAmmoMultiplier += multiplierChange;
        if (PlayStateMaster.s.isCombatInProgress()) {
            curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo*2);
        } else { 
            curAmmo = maxAmmo;
        }

        if (myAmmoBar != null) {
            myAmmoBar.OnUse(curAmmo);
            myAmmoBar.OnReload(false, curAmmo);
        }
    }

    [ReadOnly]
    public GameObject myUINoAmmoWarningThing;
    void UpdateModuleState() {

        if (myUINoAmmoWarningThing == null) {
            myUINoAmmoWarningThing = Instantiate(LevelReferences.s.noAmmoWarning, LevelReferences.s.uiDisplayParent);
            myUINoAmmoWarningThing.GetComponent<UIElementFollowWorldTarget>().SetUp(GetComponentInParent<Cart>(true).GetUITargetTransform());
        }
        
        myUINoAmmoWarningThing.SetActive(curAmmo <= 2);
    }

    public float AmmoPercent() {
        return curAmmo / maxAmmo;
    }


    public void ActivateForCombat() {
        this.enabled = true;

        Reload(-1,false);

        UpdateModuleState();
    }

    public void ActivateForShopping() {
        ActivateForCombat();
    }


    public void Disable() {
        this.enabled = false;
        curAmmo = 0;
        UpdateModuleState();
        if(myAmmoBar != null)
            myAmmoBar.OnUse( curAmmo);
    }


    private void OnDestroy() {
        if (myUINoAmmoWarningThing != null) {
            Destroy(myUINoAmmoWarningThing);
        }
    }
}


public interface IAmmoProvider {
    public float AvailableAmmo();
    public float AmmoCapacity();
    public void UseAmmo(float amountUsed);
}