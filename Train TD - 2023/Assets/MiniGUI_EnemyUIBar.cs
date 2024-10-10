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

    public Transform burnParent;
    public Image burnEffectTemplate;
    public List<Image> activeBurnEffects = new List<Image>();

    public Color[] burnColors = new Color[]{Color.black, Color.yellow, Color.red, Color.cyan};

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
        lastHealthPercent = -1;
        lastShieldPercent = -1;
        lastBurnPercent = -1;
        lastBurnTier = -1;
        lastFillingTier = -1;
    }
    
    public bool isVisible = true;
    public void SetVisible(bool _isVisible) {
        isVisible = _isVisible;
        gameObject.SetActive(isVisible);
    }

    private float lastHealthPercent;
    private float lastShieldPercent;
    private float lastBurnPercent;
    private int lastBurnTier;
    private int lastFillingTier;
    private void Update() {
        SetHealthBarValue();
        SetShieldBarValue();
        SetBurnBarValue();
        if (myHealth.GetHealthPercent() < 1f || myHealth.currentShields < myHealth.maxShields) {
            healthBar.gameObject.SetActive(true);
        }
    }
    
    void SetBurnBarValue() {
        var percent = myHealth.GetBurnPercent();
        var tier = myHealth.GetBurnTier();
        var fillingTier = myHealth.GetFillingBurnTier();
        if (Mathf.Approximately(percent, lastBurnPercent) && tier != lastBurnTier && fillingTier != lastFillingTier) {
            return;
        }
        lastBurnPercent = percent;
        lastBurnTier = tier;
        lastFillingTier = fillingTier;
        
        if (/*true ||*/ (tier == 0 && percent <= 0.1f)) {
            burnParent.gameObject.SetActive(false);
            return;
        } else {
            burnParent.gameObject.SetActive(true);
        }
        
        percent = Mathf.Clamp(percent, 0, 1f);

        /*var prevBurnColor = burnColors[Mathf.Clamp(tier,0,burnColors.Length-1)];
        prevBurnColor.r *= 0.8f;
        prevBurnColor.g *= 0.8f;
        prevBurnColor.b *= 0.8f;*/
        var curBurnColor = burnColors[Mathf.Clamp(fillingTier,0,burnColors.Length-1)];

        while (activeBurnEffects.Count < fillingTier) {
            activeBurnEffects.Add(Instantiate(burnEffectTemplate, burnParent));
            activeBurnEffects[^1].gameObject.SetActive(true);
        }

        while (activeBurnEffects.Count > fillingTier) {
            var toRemove = activeBurnEffects[^1];
            activeBurnEffects.RemoveAt(activeBurnEffects.Count-1);
            Destroy(toRemove);
        }

        if (activeBurnEffects.Count > 0) {
            for (int i = 0; i < activeBurnEffects.Count; i++) {
                activeBurnEffects[i].color = curBurnColor;
            }

            if (fillingTier >= tier) {
                activeBurnEffects[^1].color = Color.gray;
            }

            activeBurnEffects[^1].fillAmount = percent;
        }
    }

    void SetShieldBarValue() {
        var percent = myHealth.GetShieldPercent();
        if (Mathf.Approximately(percent, lastShieldPercent)) {
            return;
        }
        lastShieldPercent = percent;
        
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
        
        if (Mathf.Approximately(percent, lastHealthPercent)) {
            return;
        }
        lastHealthPercent = percent;

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
