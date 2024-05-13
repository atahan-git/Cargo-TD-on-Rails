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
public class MiniGUI_CartUIBar : MonoBehaviour
{
   public RectTransform mainRect;
    
    public RectTransform healthBar;
    public Image healthFill;
    
    public RectTransform maxHealthReductionBar;
    public Image maxHealthReductionFill;

    public ModuleHealth myHealth;

    public Color fullColor = Color.green;
    public Color halfColor = Color.yellow;
    public Color emptyColor = Color.red;

    private static readonly int Tiling = Shader.PropertyToID("_Tiling");

    public TMP_Text cartName;

    public void SetUp(ModuleHealth health) {
        myHealth = health;

        healthFill.material = new Material(healthFill.material);
        maxHealthReductionFill.material = new Material(maxHealthReductionFill.material);

        cartName.text = health.GetComponent<Cart>().displayName;
        
        var maxSize = mainRect.rect.width;
        var idealSize = myHealth.GetMaxHealth(false) / 2000 * maxSize;
        if (idealSize > maxSize) {
            var excess = idealSize - maxSize;
            excess = Mathf.Log(excess + 1);
            idealSize = maxSize + excess;
        }
        
        idealSize = Mathf.Clamp(idealSize, 0, 1000);

        mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, idealSize);
        Update();

        GetComponent<UIElementFollowWorldTarget>().SetUp(health.GetUITransform());
    }

    public bool isVisible = true;
    public void SetVisible(bool _isVisible) {
        isVisible = _isVisible;
        gameObject.SetActive(isVisible);
    }

    private void Update() {
        SetHealthBarValue();
        SetMaxHealthReductionValue();
    }

    void SetMaxHealthReductionValue() {
        var percent = myHealth.maxHealthReduction/myHealth.GetMaxHealth(false);
        percent = Mathf.Clamp(percent, 0, 1f);

        var totalLength = -mainRect.rect.x*2;
        //print(totalLength*(1-percent));
        
        maxHealthReductionBar.SetLeft(totalLength*(1-percent));
        maxHealthReductionFill.material.SetFloat(Tiling, myHealth.maxHealthReduction/100f);
    }
    
    void SetHealthBarValue() {
        var percent = myHealth.GetHealthPercent();
        percent = Mathf.Clamp(percent, 0, 1f);
        percent = percent.Remap(0, 1, 0.1f, 1f);

        var totalLength = mainRect.sizeDelta.x;
        
        healthBar.SetRight(totalLength*(1-percent));
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
