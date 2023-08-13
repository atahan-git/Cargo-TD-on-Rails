using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_CartValueAffector : ActivateWhenOnArtifactRow {

    public bool engineOnly;
    public bool cargoOnly;
    public bool applyGlobally;
    
    [Space]
    
    public bool glassCart = false;
    public bool canHaveNoShields = false;
    public bool reflectiveShields = false;
    public bool luckyCart = false;
    
    public float addShieldAsHpPercent = 0f;
    public float addShieldAmount = 0;
    public float healthMultiplier = 1;
    public float shieldDecayMultiplier = 1f;
    public float fireDecayRateMultiplier = 1f;

    [Space]
    
    public bool gatlinificator = false;
    public bool isHoming = false;
    
    public float fireRateMultiplier = 1;
    public float damageMultiplier = 1;
    public float ammoMultiplier = 1;

    public float addFireDamage = 0;
    public float addExplosionRange = 0;
    [Space]
    

    public int boosterRangeModification = 0;
    public float boosterEffectMultiplier = 1;
    [Space]

    public float repairEfficiency = 1;
    public float reloadEfficiency = 1;
    public float shieldUpEfficiency = 1;
    

    protected override void _Arm() {
        var targets = new List<Cart>();
        if (!applyGlobally) {
            GetComponent<Artifact>().range = 1;
            var myCart = GetComponentInParent<Cart>();

            targets.Add(myCart);
            targets.Add(Train.s.GetNextBuilding(1, myCart));
            targets.Add(Train.s.GetNextBuilding(-1, myCart));
        } else {
            GetComponent<Artifact>().range = 100;
            targets = Train.s.carts;
        }

        for (int i = 0; i < targets.Count; i++) {
            var cart = targets[i];
            if(cart == null)
                continue;

            if (engineOnly && !cart.isMainEngine) {
                return;
            }

            if (cargoOnly && (!cart.isCargo && !cart.isMysteriousCart)) {
                return;
            }

            var healthModule = cart.GetHealthModule();
            

            if (healthModule != null) {
                if (canHaveNoShields) {
                    healthModule.canHaveShields = false;
                    healthModule.maxShields = 0;
                }
                
                if (healthModule.canHaveShields) {
                    healthModule.maxShields += addShieldAsHpPercent * healthModule.maxHealth;
                    healthModule.maxShields += addShieldAmount;
                }

                if (glassCart) {
                    healthModule.glassCart = glassCart;
                    healthModule.maxHealth = 100;
                    healthModule.currentHealth = 100;
                }

                if (!healthModule.glassCart) {
                    healthModule.maxHealth *= healthMultiplier;
                    healthModule.currentHealth *= healthMultiplier;
                }

                if(reflectiveShields)
                    healthModule.reflectiveShields = reflectiveShields;

                if (luckyCart)
                    healthModule.luckyCart = luckyCart;

                healthModule.burnReductionMultiplier *= fireDecayRateMultiplier;

                healthModule.repairEfficiency += repairEfficiency - 1;
                healthModule.shieldUpEfficiency += shieldUpEfficiency - 1;
                healthModule.shieldDecayMultiplier *= shieldDecayMultiplier;
            }

            var moduleAmmo = cart.GetComponentInChildren<ModuleAmmo>();
            if (moduleAmmo != null) {
                moduleAmmo.ChangeMaxAmmo(ammoMultiplier-1);
                moduleAmmo.reloadEfficiency += reloadEfficiency - 1;
            }
            
            foreach (var gunModule in cart.GetComponentsInChildren<GunModule>()) {
                gunModule.damageMultiplier += damageMultiplier-1;
                gunModule.fireRateMultiplier += fireRateMultiplier - 1;
                gunModule.bonusBurnDamage += addFireDamage;
                if (gatlinificator) {
                    if (!gunModule.isGigaGatling) {
                        gunModule.gatlinificator = true;
                    } else {
                        gunModule.fireRateMultiplier -= fireRateMultiplier - 1;
                        gunModule.fireRateMultiplier += 1;
                    }
                }

                if (isHoming)
                    gunModule.isHoming = true;

                gunModule.explosionRangeBoost += addExplosionRange;
            }

            foreach (var booster in cart.GetComponentsInChildren<IBooster>()) {
                booster.ModifyStats(boosterRangeModification, boosterEffectMultiplier-1f);
            }
        }
    }

    protected override void _Disarm() {
        // do nothing
    }
}
