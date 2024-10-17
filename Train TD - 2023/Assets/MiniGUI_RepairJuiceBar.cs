using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_RepairJuiceBar : MonoBehaviour
{

    public RectTransform mainRect;
    public RectTransform healthBar;
    public Image healthFill;
    
    public GameObject warning;

    private static readonly int Tiling = Shader.PropertyToID("_Tiling");

    public float health;
    public float maxHealth;
    public float lerpHealth;
    public float lerpMaxHealth;
    
     public static MiniGUI_RepairJuiceBar s;
     private void Awake() {
        s = this;
    }

    private bool materialSet = false;

    public void MaxJuiceChanged() {
        maxHealth = Train.s.GetComponent<RepairJuiceTracker>().GetJuiceCapacity()*20;
        health = Train.s.GetComponent<RepairJuiceTracker>().GetCurrentJuice()*20;

        var maxSize = GetComponent<RectTransform>().rect.width;
        var idealSize = maxHealth / 4000 * maxSize;
        if (idealSize > maxSize) {
            var excess = idealSize - maxSize;
            excess = Mathf.Log(excess + 1);
            idealSize = maxSize + excess;
        }

        idealSize = Mathf.Clamp(idealSize, 130, 1000);

        mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, idealSize);
        Update();

        warning.SetActive(false);

        if (!materialSet) {
            healthFill.material = new Material(healthFill.material);
            materialSet = true;
        }
    }

    public void JuiceChanged() {
        /*var newhealth = Train.s.GetComponent<RepairJuiceTracker>().GetCurrentJuice()*10;
        
        if (Mathf.Abs(newhealth - health) < 0.1f) {
            return;
        } else {
            health = newhealth;
        }
        
        SetHealthBarValue();*/
    }


    private void Update() {
        lerpMaxHealth = Mathf.Lerp(lerpMaxHealth, maxHealth, 7 * Time.deltaTime);
        lerpHealth = Mathf.Lerp(lerpHealth, health, 7 * Time.deltaTime);
        
        health = Train.s.GetComponent<RepairJuiceTracker>().GetCurrentJuice()*20;
        SetHealthBarValue();
    }

    void SetHealthBarValue() {
        lerpMaxHealth = Mathf.Clamp(lerpMaxHealth, 1, float.MaxValue);
        var percent = lerpHealth/lerpMaxHealth;
        percent = Mathf.Clamp(percent, 0, 1f);

        var totalLength = -mainRect.rect.x*2;
        
        healthBar.SetRight(totalLength*(1-percent));
        healthFill.material.SetFloat(Tiling, lerpHealth/100f);

        warning.SetActive(health < 5);
        //warning.GetComponent<PulseAlpha>().speed = percent.Remap(0f, 0.5f, 3f, 0.2f);
    }
}
