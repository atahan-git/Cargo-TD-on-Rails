using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Utility;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_EnemyUIBar : MonoBehaviour{
    public RectTransform mainRect;
    
    public RectTransform healthBar;
    public Image healthFill;
    
    public RectTransform shieldBar;
    public Image shieldFill;

    public EnemyHealth myHealth;

    public Color fullColor = Color.green;
    public Color halfColor = Color.yellow;
    public Color emptyColor = Color.red;

    private static readonly int Tiling = Shader.PropertyToID("_Tiling");

    public void SetUp(EnemyHealth health) {
        myHealth = health;

        healthFill.material = new Material(healthFill.material);
        shieldFill.material = new Material(shieldFill.material);

        GetComponent<UIElementFollowWorldTarget>().SetUp(health.uiTransform);
    }

    private void Update() {
        SetHealthBarValue();
        SetShieldBarValue();
        if (myHealth.GetHealthPercent() < 1f || myHealth.currentShields < myHealth.maxShields) {
            healthBar.gameObject.SetActive(true);
        }
    }

    void SetShieldBarValue() {
        var percent = myHealth.GetShieldPercent();
        
        if (myHealth.maxShields <= 0) {
            shieldBar.gameObject.SetActive(false);
            return;
        } else {
            shieldBar.gameObject.SetActive(true);
        }
        
        percent = Mathf.Clamp(percent, 0, 1f);

        var totalLength = mainRect.sizeDelta.x;
        
        shieldFill.GetComponent<RectTransform>().SetRight(totalLength*(1-percent));
        shieldFill.material.SetFloat(Tiling, myHealth.currentShields/100f);
    }
    
    void SetHealthBarValue() {
        var percent = myHealth.GetHealthPercent();
        percent = Mathf.Clamp(percent, 0, 1f);
        percent = percent.Remap(0, 1, 0.1f, 1f);

        var totalLength = mainRect.sizeDelta.x;
        
        healthFill.GetComponent<RectTransform>().SetRight(totalLength*(1-percent));
        healthFill.color = GetHealthColor(percent);
        healthFill.material.SetFloat(Tiling, myHealth.currentHealth/100f);
    }

    private Color GetHealthColor(float percentage) {
        Color color;
        if (percentage > 0.5f) {
            color = Color.Lerp(halfColor, fullColor, (percentage - 0.5f) * 2);
        } else {
            color = Color.Lerp(emptyColor, halfColor, (percentage) * 2);
        }

        return color;
    }
}
