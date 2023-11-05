using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class MiniGUI_InfoCard_Health : MonoBehaviour, IBuildingInfoCard {

    public TMP_Text health;

    public bool enemyMode = false;
    [ReadOnly] public ModuleHealth healthModule;
    [ReadOnly] public EnemyHealth enemyHealth;
    public void SetUp(Cart building) {
	    healthModule = building.GetComponentInChildren<ModuleHealth>();
        
        if (healthModule == null) {
            gameObject.SetActive(false);
            return;
        }else{
            gameObject.SetActive(true);
        }
        
        Update();

        enemyMode = false;
    }

    public void SetUp(EnemyHealth enemy) {
        enemyHealth = enemy;
        enemyMode = true;
        Update();
    }

    public void SetUp(Artifact artifact) {
        gameObject.SetActive(false);
    }

    private void Update() {
        if (!enemyMode) {
            if (healthModule.invincible) {
                health.text = $"Cannot be damaged";
            } else {
                health.text = $"Health: {healthModule.currentHealth}/{healthModule.maxHealth}";
            }
        } else {
            health.text = $"Health: {enemyHealth.currentHealth}/{enemyHealth.maxHealth}";
        }
    }
}
