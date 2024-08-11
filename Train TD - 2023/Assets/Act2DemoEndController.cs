using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Act2DemoEndController : MonoBehaviour {
    public static Act2DemoEndController s;

    private void Awake() {
        s = this;
    }


    public GameObject deathCrystal;

    public int deathAct = 3;
    public void OnEnterShop()
    {
        EnemyWavesController.s.act2DemoEndNoSpawn = false;
        if (DataSaver.s.GetCurrentSave().currentRun.currentAct >= deathAct) {
            EnemyWavesController.s.act2DemoEndNoSpawn = true;
        } 
    }


    public void OnStartCombat() {
        if (DataSaver.s.GetCurrentSave().currentRun.currentAct >= deathAct) {
            var myObj = Instantiate(deathCrystal, Vector3.forward * 100, Quaternion.identity);
            myObj.GetComponent<Act2DemoEndCrystalCollision>().SetUp(50);
        }
    }


    public void CollidedWithCrystal(Vector3 collisionPoint) {
        CheatsController.s.playerIsImmune = false;
        var engine = Train.s.carts[0];
        engine.GetHealthModule().currentHealth = 0;
        engine.GetHealthModule().GetDestroyed();
        engine.GetHealthModule().UpdateHpState();
        CommonEffectsProvider.s.SpawnEffect(CommonEffectsProvider.CommonEffectType.megaDamage, collisionPoint, Quaternion.identity, VisualEffectsController.EffectPriority.Always);
        MissionLoseFinisher.s.MissionLost(MissionLoseFinisher.MissionLoseReason.endOfDemo);
    }
}
