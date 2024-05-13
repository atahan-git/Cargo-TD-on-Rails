using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossEnemyTracker : MonoBehaviour {

    public bool beingTracked = false;
    void Start()
    {
        if (BossController.s.CanSpawnBoss()) {
            beingTracked = true;
            BossController.s.IncrementBossesSpawned();
        } else {
            Destroy(gameObject,0.1f);
        }
    }

    private void OnDestroy() {
        if (BossController.s != null) {
            BossController.s.IncrementBossesKilled();
        }
    }
}
