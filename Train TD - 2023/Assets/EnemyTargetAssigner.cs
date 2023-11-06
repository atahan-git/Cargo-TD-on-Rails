using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyTargetAssigner : MonoBehaviour {
    public static EnemyTargetAssigner s;

    private void Awake() {
        s = this;
    }


    public float shootCredit;
    public float shootCreditPerSecond = 1;
    //public float shootCreditIncreasePerAct;
    public float maxShootCredit = 20;
    
    public Queue<GunModule> shootRequesters = new Queue<GunModule>();

    public TMP_Text shootCreditDisplay;
    

    // Update is called once per frame
    void Update() {
        shootCredit += shootCreditPerSecond * Time.deltaTime;
        if (shootCredit > maxShootCredit) {
            shootCredit = maxShootCredit;
        }

        if (shootCredit > 0 && shootRequesters.Count > 0) {
            DispenseShootCredits();
        }

        shootCreditDisplay.text = $"{shootCredit:F0}";
    }


    void DispenseShootCredits() {
        while (shootRequesters.TryDequeue(out GunModule result)) {
            if (result != null) {
                result.gotShootCredits = true;
                shootCredit -= result.shootCreditsUse;

                if (shootCredit < 0) {
                    return;
                }
            }
        }
        
    }

}
