using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EncounterRequirement : MonoBehaviour
{
    public Sprite icon;

    public ResourceTypes myType;
    public int amount = 50;
    public bool randomizeAmount = true;
    public bool randomizeALot = false;
    readonly float randomPercent = 0.1f;
    readonly float randomALotPercent = 0.5f;

    public void RandomizeRequirement() {
        if (randomizeAmount) {
            if (randomizeALot) {
                amount = (int)(amount * (1 + Random.Range(-randomALotPercent, randomALotPercent)));
            } else {
                amount = (int)(amount * (1 + Random.Range(-randomPercent, randomPercent)));
            }
        }
    }


    public bool CanFulfillRequirement() {
        return true;
    }

    public void UseResource() {
        //MoneyController.s.ModifyResource(myType, -amount);
    }
}
