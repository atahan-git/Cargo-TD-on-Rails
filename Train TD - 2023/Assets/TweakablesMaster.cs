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


    /*[HideInInspector]
    public UnityEvent tweakableChanged = new UnityEvent();*/


    public void ApplyTweakableChange() {
        //ResetTweakable();
        Train.s.TrainChanged();
        //tweakableChanged?.Invoke();
    }
}


[System.Serializable]
public class Tweakables {
    
    public float enemyDamageMultiplier = 1f;
    public float playerDamageMultiplier = 1f;

    public float enemyFirerateBoost = 1f;
    public float playerFirerateBoost = 1f;

    public float playerAmmoUseMultiplier = 1f;

    public Tweakables Copy() {
        var serialized = SerializationUtility.SerializeValue(this, DataFormat.Binary);
        return SerializationUtility.DeserializeValue<Tweakables>(serialized, DataFormat.Binary);
    }
}
