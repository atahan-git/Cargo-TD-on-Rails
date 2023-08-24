using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class MiniGUI_InfoCard_HealthAndWeight : MonoBehaviour, IBuildingInfoCard {

    public TMP_Text health;
    public TMP_Text weight;

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

        weight.gameObject.SetActive(true);
        weight.text = $"Size: {building.cartSize}";
        enemyMode = false;
    }

    public void SetUp(EnemyHealth enemy) {
        enemyHealth = enemy;
        
        weight.gameObject.SetActive(false);
        enemyMode = true;
        Update();
    }

    public void SetUp(Artifact artifact) {
        gameObject.SetActive(false);
    }

    private void Update() {
        if (!enemyMode) {
            health.text = $"Health: {healthModule.currentHealth}/{healthModule.maxHealth}";
        } else {
            health.text = $"Health: {enemyHealth.currentHealth}/{enemyHealth.maxHealth}";
        }
    }
}
