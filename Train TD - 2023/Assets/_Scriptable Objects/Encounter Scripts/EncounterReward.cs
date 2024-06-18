using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EncounterReward : MonoBehaviour {
    public Sprite icon;

    /*public ResourceTypes myType;
    public int amount = 0;
    public bool randomizeAmount = true;*/
    readonly float randomPercent = 0.1f;

    public Cart building;


    public int damageTrain = 0;

    public bool ambush = false;

    public void RandomizeReward() {
        /*if (randomizeAmount) {
            amount = (int)(amount * (1 + Random.Range(-randomPercent, randomPercent)));
        }*/
    }

    public void GainReward() {
        if (damageTrain <= 0) {
            if (building != null) {
                //UpgradesController.s.AddModulesToAvailableModules(building.uniqueName, amount);
                var cart = Instantiate(building);
                Train.s.AddCartAtIndex(Train.s.carts.Count, cart);
            } else {
                //MoneyController.s.ModifyResource(myType, amount);
            }
        } else if (ambush) {
            EncounterController.s.doAmbush = true;
        } else {
            var healths = Train.s.GetComponentsInChildren<ModuleHealth>();

            var damagePercent = damageTrain * 0.25f;
            var damageChance = 0.3f + (damageTrain * 0.1f);

            for (int i = 0; i < healths.Length; i++) {
                if (Random.value > damageChance) {
                    healths[i].DealDamage(healths[i].GetMaxHealth() * damagePercent, null);
                    var prefab = LevelReferences.s.mediumDamagePrefab;
                    if (damageTrain == 1)
                        prefab = LevelReferences.s.smallDamagePrefab;
                    if (damageTrain >= 3)
                        prefab = LevelReferences.s.bigDamagePrefab;

                    Instantiate(prefab, healths[i].transform.position, Quaternion.identity);
                }
            }

            Train.s.SaveTrainState();
        }
    }
}
