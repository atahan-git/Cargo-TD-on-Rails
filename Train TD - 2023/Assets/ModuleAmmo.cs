using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class ModuleAmmo : MonoBehaviour, IActiveDuringCombat, IActiveDuringShopping, IResetState, IAmmoProvider, IDisabledState {

    [ShowInInspector]
    public float curAmmo { get; private set; }
    public int _maxAmmo = 100;

    [ShowInInspector]
    public int maxAmmo {
        get { return Mathf.RoundToInt(_maxAmmo * currentAffectors.maxAmmoMultiplier); }
    }

    public PhysicalAmmoBar myAmmoBar;
    public Cart myCart;
    
    public Affectors currentAffectors;

    [Serializable]
    public class Affectors {
        public float maxAmmoMultiplier = 1;
        public float reloadEfficiency = 1;
        public float reloadOverTime = 0;
        public int explosionResistance = 0;
    }

    public bool dontLoseAmmoInThisDisable = false;

    public void ResetState() {
        currentAffectors = new Affectors();

        myAmmoBar = GetComponentInChildren<PhysicalAmmoBar>();
        myAmmoBar.OnAmmoTypeChange();
        
        ChangeMaxAmmo();
        UpdateModuleState();
    }

    private void Start() {
        myAmmoBar = GetComponentInChildren<PhysicalAmmoBar>();
        myAmmoBar.OnAmmoTypeChange();
        myCart = GetComponentInParent<Cart>();
        UpdateModuleState();
    }

    public float AvailableAmmo() {
        return curAmmo;
    }

    public float AmmoCapacity() {
        return maxAmmo;
    }

    public void UseAmmo(float usedAmount) {
        curAmmo -= usedAmount*TweakablesMaster.s.GetPlayerAmmoUseMultiplier();
        
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
                    amount = 0;
                    if(curAmmo < maxAmmo)
                        amount = maxAmmo-curAmmo;
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
    
    public void ChangeMaxAmmo() {
        if (PlayStateMaster.s.isCombatInProgress()) {
            //curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo*2);
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
        if(!PlayStateMaster.s.isCombatInProgress())
            Reload(-1,false);
        
        UpdateModuleState();
    }

    public void ActivateForShopping() {
        ActivateForCombat();
    }


    public void Disable() {
        this.enabled = false;
        UpdateModuleState();
        if(myAmmoBar != null)
            myAmmoBar.OnUse( curAmmo);
    }


    private void OnDestroy() {
        if (myUINoAmmoWarningThing != null) {
            Destroy(myUINoAmmoWarningThing);
        }
    }

    public void CartDisabled() {
        if (dontLoseAmmoInThisDisable) {
            dontLoseAmmoInThisDisable = false;
        } else {
            if(currentAffectors.explosionResistance <= 0)
                curAmmo = 0;
        }

        UpdateModuleState();
        if(myAmmoBar != null)
            myAmmoBar.OnUse( curAmmo);
    }


    public float autoReloadCharge = 0;
    public void Update() {
        if (!myCart.isDestroyed) {
            autoReloadCharge += currentAffectors.reloadOverTime * Time.deltaTime;
            if (autoReloadCharge >= 1) {
                autoReloadCharge -= 1;
                Reload(1);
            }
        }
    }

    public void CartEnabled() {
        // do nothing
    }
}


public interface IAmmoProvider {
    public float AvailableAmmo();
    public float AmmoCapacity();
    public void UseAmmo(float amountUsed);
}