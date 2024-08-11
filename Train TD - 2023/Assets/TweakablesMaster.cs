using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

public class TweakablesMaster : MonoBehaviour {
    public static TweakablesMaster s;
    
    private Tweakables _myTweakables;
    public Tweakables myTweakables;

    public Tweakables difficultyTweakables = new Tweakables();

    private void Awake() {
        s = this;
        _myTweakables = myTweakables.Copy();
    }

    public void ResetTweakable() {
        myTweakables = _myTweakables.Copy();
    }


    /*[HideInInspector]
    public UnityEvent tweakableChanged = new UnityEvent();*/


    public void ApplyTweakableChange() {
        //ResetTweakable();
        Train.s.TrainChanged();
        //tweakableChanged?.Invoke();
    }


    public float GetEnemyDamageMultiplier() {
        return myTweakables.enemyDamageMultiplier * difficultyTweakables.enemyDamageMultiplier;
    }

    public float GetPlayerDamageMultiplier() {
        return myTweakables.playerDamageMultiplier * difficultyTweakables.playerDamageMultiplier;
    }

    public float GetEnemyFirerateBoost() {
        return myTweakables.enemyFirerateBoost * difficultyTweakables.enemyFirerateBoost;
    }

    public float GetPlayerFirerateBoost() {
        return myTweakables.playerFirerateBoost * difficultyTweakables.playerFirerateBoost;
    }
    
    public float GetPlayerAmmoUseMultiplier() {
        return myTweakables.playerAmmoUseMultiplier * difficultyTweakables.playerAmmoUseMultiplier;
    }
    
    public float GetPlayerRepairTimeMultiplier() {
        return myTweakables.playerRepairTimeMultiplier * difficultyTweakables.playerRepairTimeMultiplier;
    }
    
    public float GetEnemyBudgetMultiplier() {
        return myTweakables.enemyBudgetMultiplier * difficultyTweakables.enemyBudgetMultiplier;
    }
    
    public int GetExtraUniqueGearBudget() {
        return myTweakables.extraUniqueGear * difficultyTweakables.extraUniqueGear;
    }
    
    public float GetTrainSpeedMultiplier() {
        return myTweakables.trainSpeedMultiplier * difficultyTweakables.trainSpeedMultiplier;
    }

    public float GetShootCreditsAddMultiplier() {
        return myTweakables.enemyShootCreditsAddMultiplier * difficultyTweakables.enemyShootCreditsAddMultiplier;
    }
    
    public int GetMaxShootCreditsAdd() {
        return myTweakables.enemyMaxShootCreditsAdd * difficultyTweakables.enemyMaxShootCreditsAdd;
    }
}


[System.Serializable]
public class Tweakables {
    
    public float enemyDamageMultiplier = 1f;
    public float playerDamageMultiplier = 1f;

    public float enemyFirerateBoost = 1f;
    public float playerFirerateBoost = 1f;

    public float playerAmmoUseMultiplier = 1f;
    public float playerRepairTimeMultiplier = 1f;

    public float enemyBudgetMultiplier = 1f;
    public int extraUniqueGear = 0;
    
    public float trainSpeedMultiplier = 1;
    public float enemyShootCreditsAddMultiplier = 1f;
    public int enemyMaxShootCreditsAdd = 0;

    public Tweakables Copy() {
        var serialized = SerializationUtility.SerializeValue(this, DataFormat.Binary);
        return SerializationUtility.DeserializeValue<Tweakables>(serialized, DataFormat.Binary);
    }
}
