using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpeedToEnemyDamageModifiersController : MonoBehaviour {
    public static PlayerSpeedToEnemyDamageModifiersController s;

    private void Awake() {
        s = this;
    }
    // speed ranges
    // <2 blue
    // 2-4 green
    // 4-6 orange
    // 6+ red

    public float criticalChanceAtZeroSpeed = 0.5f;
    public float missChanceAtMaxOrangeSpeed = 0.5f;


    public float GetCriticalChance() {
        var speed = LevelReferences.s.speed;
        if (speed >= 2) {
            return 0f;
        } else {
            speed = speed.Remap(0, 2, 1, 0);
            return speed * criticalChanceAtZeroSpeed;
        }
    }

    public float GetMissChance() {
        var speed = LevelReferences.s.speed;
        if (speed <= 4) {
            return 0f;
        } else if (speed >= 6) {
            return missChanceAtMaxOrangeSpeed;
        }else{
            speed = speed.Remap(4, 6, 0, 1);
            return speed * missChanceAtMaxOrangeSpeed;
        }
    }
}
