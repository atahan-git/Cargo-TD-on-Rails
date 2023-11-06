using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_CartUIBar : MonoBehaviour {

    public Image icon;

    public RectTransform mainRect;
    
    public RectTransform ammoBar;
    public RectTransform healthBar;
    public Image healthFill;
    
    public RectTransform shieldBar;
    public Image shieldFill;
    
    public Image coloredButton;
    public RectTransform engineBoost;


    public Cart myCart;
    public ModuleHealth myHealth;
    public ModuleAmmo myAmmo;

    public bool isAmmo = false;
    
    public Color fullColor = Color.green;
    public Color halfColor = Color.yellow;
    public Color emptyColor = Color.red;

    public float ammoMinHeight;
    public float ammoMaxHeight;

    
    public float boostMinHeight;
    public float boostMaxHeight;
    
    public float cartLengthToWidth = 100;

    public GameObject warning;

    public bool showWarning = false;

    public bool isBoost = false;
    private static readonly int Tiling = Shader.PropertyToID("_Tiling");

    //private Material _material;

    private bool showAnyHealth = false;
    
    public void SetUp(Cart cart, ModuleHealth moduleHealth, ModuleAmmo moduleAmmo) {
        myCart = cart;
        myHealth = moduleHealth;
        
        
        myAmmo = moduleAmmo;
        isAmmo = myAmmo != null;
        var boostable = cart.GetComponentInChildren<EngineBoostable>();
        isBoost = boostable != null;
        ammoBar.gameObject.SetActive(isAmmo);
        var coloredButtonComponent = cart.GetComponentInChildren<IShowButtonOnCartUIDisplay>();
        if (coloredButtonComponent != null) {
            coloredButton.gameObject.SetActive(true);
            coloredButton.color = coloredButtonComponent.GetColor();
        } else {
            coloredButton.gameObject.SetActive(false);
        }

        engineBoost.gameObject.SetActive(isBoost);
        
        mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cartLengthToWidth*myCart.length);
        Update();

        icon.sprite = myCart.Icon;
        UpdateAllSiblingPositions();
        
        warning.SetActive(false);

        showWarning = myCart.isMysteriousCart || myCart.isMainEngine;

        healthFill.material = new Material(healthFill.material);
        shieldFill.material = new Material(shieldFill.material);
        
        if (myHealth.invincible) {
            healthBar.gameObject.SetActive(false);
            shieldBar.gameObject.SetActive(false);
            showAnyHealth = false;
        } else {
            showAnyHealth = true;
        }
    }

    void UpdateAllSiblingPositions() {
        var allBars = transform.parent.GetComponentsInChildren<MiniGUI_CartUIBar>();

        for (int i = 0; i < allBars.Length; i++) {
            allBars[i].transform.SetSiblingIndex((allBars.Length - 1) - allBars[i].myCart.trainIndex);
        }
    }

    private void Update() {
        if (showAnyHealth) {
            SetHealthBarValue();
            SetShieldBarValue();
        }

        if (isAmmo)
            SetAmmoBarValue();
        if (isBoost)
            SetBoostBarValue();
    }

    public float shield;
    public float maxShield;
    void SetShieldBarValue() {
        if (PlayStateMaster.s.isCombatInProgress()) {
            shield = myHealth.currentShields;
            maxShield = myHealth.maxShields;
        } else {
            shield = Mathf.Lerp(shield, myHealth.currentShields, 7 * Time.deltaTime);
            maxShield = Mathf.Lerp(maxShield, myHealth.maxShields, 7 * Time.deltaTime);
        }
        
        if (myHealth.maxShields <= 0) {
            shieldBar.gameObject.SetActive(false);
            return;
        } else {
            shieldBar.gameObject.SetActive(true);
        }
        
        var percent = shield/maxShield;
        percent = Mathf.Clamp(percent, 0, 1f);

        var totalLength = mainRect.sizeDelta.x;
        
        shieldBar.SetRight(totalLength*(1-percent));
        shieldFill.material.SetFloat(Tiling, shield/100f);
    }


    public float health;
    public float maxHealth;
    void SetHealthBarValue() {
        if (PlayStateMaster.s.isCombatInProgress()) {
            health = myHealth.currentHealth;
            maxHealth = myHealth.maxHealth;
        } else {
            health = Mathf.Lerp(health, myHealth.currentHealth, 7 * Time.deltaTime);
            maxHealth = Mathf.Lerp(maxHealth, myHealth.maxHealth, 7 * Time.deltaTime);
        }

        var percent = health/maxHealth;
        percent = Mathf.Clamp(percent, 0, 1f);

        var totalLength = mainRect.sizeDelta.x;
        
        healthBar.SetRight(totalLength*(1-percent));
        healthFill.color = GetHealthColor(percent);
        healthFill.material.SetFloat(Tiling, health/100f);

        if (showWarning) {
            warning.SetActive(percent < 0.5f);
            warning.GetComponent<PulseAlpha>().speed = percent.Remap(0f, 0.5f, 3f, 0.2f);
        }
    }

    void SetAmmoBarValue() {
        var percent = myAmmo.AmmoPercent();
        percent = Mathf.Clamp(percent, 0, 1f);
        
        ammoBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(ammoMinHeight, ammoMaxHeight, percent));
    }
    
    void SetBoostBarValue() {
        var percent = SpeedController.s.boostGraphicPercent;
        engineBoost.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, percent.Remap(0,1,boostMinHeight, boostMaxHeight));
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

    [Button]
    public void DebugSetHP(float hp) {
        var percent = hp;
        var totalLength = mainRect.sizeDelta.x;
        print(totalLength);
        
        healthBar.SetRight(totalLength*(1-percent));
        healthBar.GetComponent<Image>().color = GetHealthColor(percent);
    }
    
    [Button]
    public void DebugSetMaxHP(float maxHp) {
        var totalLength = mainRect.sizeDelta.x;

        healthFill.pixelsPerUnitMultiplier = maxHp.Remap(0, 100, 0, 4.61f) * (1/(totalLength / 55.53f));
    }
    
    [Button]
    public void DebugSetAmmo(float ammo) {
        var percent = ammo;
        
        ammoBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(ammoMinHeight, ammoMaxHeight, percent));
    }

    public void ClickRepair() {
        PlayerWorldInteractionController.s.UIRepair(myCart);
    }

    public void ClickButton() {
        PlayerWorldInteractionController.s.CartHPUIButton(myCart);
    }
}
