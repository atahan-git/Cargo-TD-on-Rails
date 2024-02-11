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

    private void Awake() {
        s = this;
        _myTweakables = myTweakables.Copy();
    }

    public void ResetTweakable() {
        myTweakables = _myTweakables.Copy();
    }


    [HideInInspector]
    public UnityEvent tweakableChanged = new UnityEvent();


    public void ApplyTweakableChange() {
        CalculateArmorAndHealthValues();
        tweakableChanged?.Invoke();
        
        //myTweakables.GetType().GetField("yeet").SetValue();
    }


    private void CalculateArmorAndHealthValues() {
        var saveData = DataSaver.s.GetCurrentSave();
        ResetTweakable();
        myTweakables.playerMagazineSizeMultiplier *= 0.7f + (saveData.ammoUpgradesBought * 0.15f);
    }
}


[System.Serializable]
public class Tweakables {
    public float scrapEnemyRewardMultiplier = 1f;

    
    public float enemyDamageMultiplier = 1f;
    public float playerDamageMultiplier = 1f;

    public float enemyFirerateBoost = 1f;
    public float playerFirerateBoost = 1f;

    
    public float playerMagazineSizeMultiplier = 1f;
    public float gunSteamUseMultiplier = 1f;
    
    public float engineOverloadDamageMultiplier = 1f;

    public Tweakables Copy() {
        var serialized = SerializationUtility.SerializeValue(this, DataFormat.Binary);
        return SerializationUtility.DeserializeValue<Tweakables>(serialized, DataFormat.Binary);
    }
}
