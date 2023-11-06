using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class ModuleAmmo : MonoBehaviour, IActiveDuringCombat, IActiveDuringShopping, IResetState {

    [ShowInInspector]
    public float curAmmo { get; private set; }
    public int _maxAmmo = 100;
    public float maxAmmoMultiplier = 1f;

    public int maxAmmo {
        get { return Mathf.RoundToInt(_maxAmmo * maxAmmoMultiplier); }
    }
    
    public float ammoPerBarrage = 1;
    public float ammoPerBarrageMultiplier = 1;

    public GunModule[] myGunModules;
    public RoboRepairModule[] myRoboRepairModules;

    private bool listenerAdded = false;

    float AmmoUseWithMultipliers() {
        var ammoUse = ammoPerBarrage * ammoPerBarrageMultiplier;

        /*if (myGunModule.beingDirectControlled)
            ammoUse /= DirectControlMaster.s.directControlAmmoConservationBoost;*/

        ammoUse /= TweakablesMaster.s.myTweakables.playerMagazineSizeMultiplier;

        return ammoUse;
    }

    public void ResetState(int level) {
        ammoPerBarrageMultiplier = 1;
        maxAmmoMultiplier = 1;
        reloadEfficiency = 1;

        maxAmmoMultiplier = 0.7f + (DataSaver.s.GetCurrentSave().metaProgress.ammoUpgradesBought * 0.15f);
        
        UpdateConnectedModules();
        
        ChangeMaxAmmo(0);
        UpdateModuleState();
    }

    private void Start() {
        UpdateModuleState();
    }

    public void UseAmmo() {
        curAmmo -= AmmoUseWithMultipliers();
        
        curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo);
        UpdateModuleState();
        OnUse?.Invoke();
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
                curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo);

                UpdateModuleState();
                OnReload?.Invoke(showEffect);
                if(showEffect)
                    roboRepairInstantRepair?.Invoke(true);
            }
        } else {
            if(showEffect)
                roboRepairInstantRepair?.Invoke(false);
        }
    }

    public void SetAmmo(int amount) {
        curAmmo = amount;
        curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo);

        UpdateConnectedModules();

        UpdateModuleState();
        OnReload?.Invoke(false);
    }
    
    public void ChangeMaxAmmo(float multiplierChange) {
        maxAmmoMultiplier += multiplierChange;
        if (PlayStateMaster.s.isCombatInProgress()) {
            curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo);
        } else {
            if(PlayerWorldInteractionController.s.autoReloadAtStation)
                curAmmo = maxAmmo;
            else
                curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo);
        }

        OnUse?.Invoke();
        OnReload?.Invoke(false);
    }

    public UnityEvent OnUse;
    public UnityEvent<bool> OnReload;
    public UnityEvent<bool> roboRepairInstantRepair;
    public bool hasAmmo => curAmmo >= AmmoUseWithMultipliers();

    [ReadOnly]
    public GameObject myUINoAmmoWarningThing;
    void UpdateModuleState() {

        UpdateConnectedModules();
        

        if (myUINoAmmoWarningThing == null) {
            myUINoAmmoWarningThing = Instantiate(LevelReferences.s.noAmmoWarning, LevelReferences.s.uiDisplayParent);
            myUINoAmmoWarningThing.GetComponent<UIElementFollowWorldTarget>().SetUp(GetComponentInParent<Cart>(true).GetUITargetTransform());
        }
        
        myUINoAmmoWarningThing.SetActive(!hasAmmo);
        

        /*if (GetComponent<EngineModule>())
            GetComponent<EngineModule>().hasFuel = curAmmo > 0;*/

        /*if (!hasAmmo) {
            isFire = false;
            isSticky = false;
            isExplosive = false;
            
            for (int i = 0; i < myGunModules.Length; i++) {
                myGunModules[i].isFire = isFire;
                myGunModules[i].isSticky = isSticky;
                myGunModules[i].isExplosive = isExplosive;
            }
            OnAmmoTypeChange?.Invoke();
        }*/
    }

    public float AmmoPercent() {
        return curAmmo / maxAmmo;
    }


    public void ActivateForCombat() {
        this.enabled = true;

        Reload(-1,false);

        myGunModules = GetComponentsInChildren<GunModule>();
        myRoboRepairModules = GetComponentsInChildren<RoboRepairModule>();
        if (!listenerAdded) {
            for (int i = 0; i < myGunModules.Length; i++) {
                myGunModules[i].barrageShot.AddListener(UseAmmo);
            }

            for (int i = 0; i < myRoboRepairModules.Length; i++) {
                myRoboRepairModules[i].OnRepaired.AddListener(UseAmmo);
                roboRepairInstantRepair.AddListener(myRoboRepairModules[i].InstantRepair);
            }
            
            listenerAdded = true;
        }

        UpdateModuleState();
    }

    public void ActivateForShopping() {
        ActivateForCombat();
    }


    public void Disable() {
        this.enabled = false;
        myGunModules = GetComponentsInChildren<GunModule>();
        curAmmo = 0;
        UpdateModuleState();
        OnUse?.Invoke();
    }


    private bool isSetup = false;
    void UpdateConnectedModules() {
        if (!isSetup) {
            myGunModules = GetComponentsInChildren<GunModule>();
            myRoboRepairModules = GetComponentsInChildren<RoboRepairModule>();
            isSetup = true;
        }

        var _hasAmmo = hasAmmo;
        for (int i = 0; i < myGunModules.Length; i++) {
            myGunModules[i].hasAmmo = _hasAmmo;
        }

        for (int i = 0; i < myRoboRepairModules.Length; i++) {
            myRoboRepairModules[i].hasAmmo = _hasAmmo;
        }
    }
    

    private void OnDestroy() {
        if (myUINoAmmoWarningThing != null) {
            Destroy(myUINoAmmoWarningThing);
        }
    }
}
