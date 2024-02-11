using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EasterEggController : MonoBehaviour {
    public static EasterEggController s;

    private void Awake() {
        s = this;
    }

    public int minimumXPRequiredForEasterEggActivation = 20;

    
    public enum EasterEggChances {
        rare3, rare4, rare5
    }

    public bool GetEasterEggDisplay(EasterEggChances type) {
        if (DataSaver.s.GetCurrentSave().castlesTraveled < minimumXPRequiredForEasterEggActivation) {
            return false;
        }

        var roll = Random.value;
        
        switch (type) {
            case EasterEggChances.rare3:
                return roll < (1f/Mathf.Pow(10,3));
            case EasterEggChances.rare4:
                return roll < (1f/Mathf.Pow(10,4));
            case EasterEggChances.rare5:
                return roll < (1f/Mathf.Pow(10,5));
            default:

                return false;
        }
    }
}
