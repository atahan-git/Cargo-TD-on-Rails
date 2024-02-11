using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemyAmmo : MonoBehaviour, IAmmoProvider
{

    [ShowInInspector]
    public float curAmmo { get; private set; }
    public int _maxAmmo = 15;
    public float maxAmmoMultiplier = 1f;


    
    public int maxAmmo {
        get { return Mathf.RoundToInt(_maxAmmo * maxAmmoMultiplier); }
    }

    public PhysicalAmmoBar myAmmoBar;

    /*public void ResetState(int level) {
        maxAmmoMultiplier = 1;
        reloadEfficiency = 1;

        //maxAmmoMultiplier = 0.7f + (DataSaver.s.GetCurrentSave().ammoUpgradesBought * 0.15f);

        ChangeMaxAmmo(0);
        myAmmoBar.OnReload(false,curAmmo);
        myAmmoBar.OnUse(curAmmo);
    }*/

    private void Start() {
        myAmmoBar = GetComponentInChildren<PhysicalAmmoBar>();

        Reload(-1);

        GetComponentInParent<AmmoTracker>().ammoProviders.Add(this);

    }
    
    public float AvailableAmmo() {
        if (filled) {
            return curAmmo;
        } else {
            return 0f;
        }
    }
    

    public void UseAmmo(float usedAmount) {
        curAmmo -= usedAmount;
        
        curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo);
        myAmmoBar.OnUse(curAmmo);

        if (curAmmo < 1) {
            curAmmo = 0f;
            Invoke(nameof(SetUnfilled), 5f);
        }
    }



    public float reloadEfficiency = 1;

    [Button]
    public void Reload(float amount = -1) {
        amount *= reloadEfficiency;
        if (curAmmo < maxAmmo) {
            if (amount < 0) {
                amount = maxAmmo;
            }

            curAmmo += amount;
            curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo);

            myAmmoBar.OnReload(true, curAmmo);
            
            if (curAmmo >= maxAmmo) {
                Invoke(nameof(SetFilled),3);
            }
        }
    }

    void SetUnfilled() {
        filled = false;
    }
    
    void SetFilled() {
        filled = true;
    }

    /*public void SetAmmo(int amount) {
        curAmmo = amount;
        curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo);

        myAmmoBar.OnReload(false, curAmmo);
    }*/
    
    /*public void ChangeMaxAmmo(float multiplierChange) {
        maxAmmoMultiplier += multiplierChange;
        if (PlayStateMaster.s.isCombatInProgress()) {
            curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo);
        } else {
            if(PlayerWorldInteractionController.s.autoReloadAtStation)
                curAmmo = maxAmmo;
            else
                curAmmo = Mathf.Clamp(curAmmo, 0, maxAmmo);
        }

        myAmmoBar.OnUse( curAmmo);
        myAmmoBar.OnReload(false, curAmmo);
    }*/


    
    public bool filled = true;
    public float fillPerSecond = 1.5f;
    private void Update() {
        if (!filled) {
            Reload( fillPerSecond * Time.deltaTime);
        }
    }
}
