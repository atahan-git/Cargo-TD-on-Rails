using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_TrainOverallHealthBar : MonoBehaviour {
    public RectTransform mainRect;
    public RectTransform healthBar;
    public Image healthFill;
    public RectTransform maxHealthReductionBar;
    public Image maxHealthReductionFill;
    
    public Color fullColor = Color.green;
    public Color halfColor = Color.yellow;
    public Color emptyColor = Color.red;

    public GameObject warning;

    private static readonly int Tiling = Shader.PropertyToID("_Tiling");

    public float health;
    public float maxHealth;
    public float maxHealthReduction;
    public float lerpHealth;
    public float lerpMaxHealth;

    public static MiniGUI_TrainOverallHealthBar s;

    private void Awake() {
        s = this;
    }

    private bool materialSet = false;

    public void MaxHealthChanged() {
        maxHealth = 0;
        health = 0;
        maxHealthReduction = 0;
        for (int i = 0; i < Train.s.carts.Count; i++) {
            var cart = Train.s.carts[i].GetHealthModule();
            if (!cart.invincible) {
                maxHealth += cart.GetMaxHealth(false);
                maxHealthReduction += cart.maxHealthReduction;
                health += cart.currentHealth;
            }
        }



        var maxSize = GetComponent<RectTransform>().rect.width;
        var idealSize = maxHealth / 7000 * maxSize;
        if (idealSize > maxSize) {
            var excess = idealSize - maxSize;
            excess = Mathf.Log(excess + 1);
            idealSize = maxSize + excess;
        }

        idealSize = Mathf.Clamp(idealSize, 0, 1000);

        mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, idealSize);
        Update();

        warning.SetActive(false);

        if (!materialSet) {
            healthFill.material = new Material(healthFill.material);
            maxHealthReductionFill.material = new Material(maxHealthReductionFill.material);
            materialSet = true;
        }
    }

    public void HealthChanged() {
        var newhealth = 0f;
        maxHealthReduction = 0;
        for (int i = 0; i < Train.s.carts.Count; i++) {
            var cart = Train.s.carts[i].GetHealthModule();
            if (!cart.invincible) {
                newhealth += cart.currentHealth;
                maxHealthReduction += cart.maxHealthReduction;
            }
        }

        SetMaxHealthReductionValue();
        
        if (Math.Abs(newhealth - health) < 0.1f) {
            return;
        } else {
            health = newhealth;
        }


        if (PlayStateMaster.s.isCombatInProgress()) {
            var percent = lerpHealth / lerpMaxHealth;
            VignetteController.s.SetVignette(1 - Mathf.Clamp01(percent * 2));
        }

        changeAlphaLerp = 1;
        SetHealthBarValue();
    }


    public float changeAlphaLerp = 0.25f;
    private void Update() {
        lerpMaxHealth = Mathf.Lerp(lerpMaxHealth, maxHealth, 7 * Time.deltaTime);
        lerpHealth = Mathf.Lerp(lerpHealth, health, 7 * Time.deltaTime);
        if (PlayStateMaster.s.isCombatInProgress()) {
            changeAlphaLerp = Mathf.Lerp(changeAlphaLerp, 0.25f, Time.deltaTime);
        } else {
            changeAlphaLerp = Mathf.Lerp(changeAlphaLerp, 1, Time.deltaTime);
        }

        SetHealthBarValue();
    }



    void SetHealthBarValue() {
        lerpMaxHealth = Mathf.Clamp(lerpMaxHealth, 1, float.MaxValue);
        var percent = lerpHealth/lerpMaxHealth;
        percent = Mathf.Clamp(percent, 0, 1f);

        var totalLength = -mainRect.rect.x*2;
        //print(totalLength*(1-percent));
        
        healthBar.SetRight(totalLength*(1-percent));
        var healthColor = GetHealthColor(percent);
        healthColor.a = changeAlphaLerp;
        healthFill.color = healthColor;
        healthFill.material.SetFloat(Tiling, lerpHealth/100f);

        warning.SetActive(percent < 0.5f);
        warning.GetComponent<PulseAlpha>().speed = percent.Remap(0f, 0.5f, 3f, 0.2f);
    }
    
    void SetMaxHealthReductionValue() {
        var percent = maxHealthReduction/maxHealth;
        percent = Mathf.Clamp(percent, 0, 1f);

        var totalLength = -mainRect.rect.x*2;
        //print(totalLength*(1-percent));
        
        maxHealthReductionBar.SetLeft(totalLength*(1-percent));
        maxHealthReductionFill.material.SetFloat(Tiling, maxHealthReduction/100f);
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
