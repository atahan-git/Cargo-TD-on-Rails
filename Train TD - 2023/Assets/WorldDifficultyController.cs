using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class WorldDifficultyController : MonoBehaviour {
    public static WorldDifficultyController s;
    private void Awake() {
        s = this;
    }

    public float enemyDamageIncreasePerLevel = 0.2f;
    public float enemyHealthIncreasePerLevel = 0.2f;


    public float baseDamageIncrease = 0.6f;
    [ReadOnly]
    public float currentDamageIncrease;
    [ReadOnly]
    public float currentHealthIncrease;

    public int playerAct;
    public int playerStar;

    public UnityEvent OnDifficultyChanged = new UnityEvent();

    public void OnShopEntered() {
        CalculateDifficulty();
    }

    [Button]
    public void CalculateDifficulty() {
        playerAct = 1;
        currentDamageIncrease = ((playerAct-1)*5 + playerStar) * enemyDamageIncreasePerLevel + baseDamageIncrease;
        currentHealthIncrease = ((playerAct-1)*5 + playerStar) * enemyHealthIncreasePerLevel;
        OnDifficultyChanged?.Invoke();
    }

}
