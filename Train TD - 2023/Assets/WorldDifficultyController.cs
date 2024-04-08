using System;
using System.Collections;
using System.Collections.Generic;
using Borodar.FarlandSkies.LowPoly;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class WorldDifficultyController : MonoBehaviour {
    public static WorldDifficultyController s;
    private void Awake() {
        s = this;
    }


    public int curLevel;
    
    public float enemyDamageIncreasePerLevel = 0.2f;
    public float enemyHealthIncreasePerLevel = 0.2f;

    public float damageIncreaseInterval = 60;

    public TMP_Text infoText;

    public float baseDamageMultiplier = 0.6f;
    public float baseHealthMultiplier = 0.6f;
    [ReadOnly]
    public float currentDamageMultiplier;
    [ReadOnly]
    public float currentHealthMultiplier;

    public UnityEvent OnDifficultyChanged = new UnityEvent();

    public float combatStartTime;

    public float baseAmbientLightStrength;
    public float baseSunlightStrength;
    public Color baseFogColor;
    
    public int totalDarknessLevel = 10;
    
    private void Start() {
        infoText.text = "";

        baseAmbientLightStrength = RenderSettings.ambientIntensity;
        baseSunlightStrength = RenderSettings.sun.intensity;
        baseFogColor = RenderSettings.fogColor;
    }

    public void OnShopEntered() {
        OnCombatStart();
    }

    private void Update() {
        if (PlayStateMaster.s.isCombatInProgress()) {
            var curCombatTime = GetMissionTime();
            var newLevel = Mathf.FloorToInt(curCombatTime / damageIncreaseInterval);

            if (newLevel > curLevel) {
                curLevel = newLevel;
                CalculateDifficulty();
            }

            infoText.text = $"Enemy dmg x{currentDamageMultiplier}\nEnemy hp x{currentHealthMultiplier}\nStage {curLevel} - {ExtensionMethods.FormatTime(curCombatTime)}";
        }
    }

    public float GetMissionTime() {
        return Time.timeSinceLevelLoad - combatStartTime;
    }

    [Button]
    public void CalculateDifficulty() {
        currentDamageMultiplier = curLevel * enemyDamageIncreasePerLevel + baseDamageMultiplier;
        currentHealthMultiplier = curLevel * enemyHealthIncreasePerLevel + baseHealthMultiplier;
        OnDifficultyChanged?.Invoke();

        //StartCoroutine(LerpLights());
    }


    IEnumerator LerpLights() {
        var transitionPercent = 0f;

        SkyboxController.Instance.AdjustFogColor = false;

        var targetDarknessPercent = curLevel / (float)totalDarknessLevel;
        targetDarknessPercent = Mathf.Clamp(targetDarknessPercent, 0,0.9f);

        var prevAmbientIntensity = RenderSettings.ambientIntensity;
        var targetAmbientIntensity = Mathf.Lerp(baseAmbientLightStrength, 0, targetDarknessPercent);
        
        
        var prevSunlightIntensity = RenderSettings.sun.intensity;
        var targetSunlightIntensity = Mathf.Lerp(baseSunlightStrength, 0, targetDarknessPercent);

        var prevSkyboxExposure = SkyboxController.Instance.Exposure;
        var targetSkyboxExposure = Mathf.Lerp(1, 0, targetDarknessPercent);

        var prevFogColor = RenderSettings.fogColor;
        var targetFogColor = Color.Lerp(baseFogColor, Color.black, targetDarknessPercent);

        while (transitionPercent < 1f){
            RenderSettings.ambientIntensity = Mathf.Lerp(prevAmbientIntensity, targetAmbientIntensity, transitionPercent);
            RenderSettings.sun.intensity = Mathf.Lerp(prevSunlightIntensity, targetSunlightIntensity, transitionPercent);
            SkyboxController.Instance.Exposure = Mathf.Lerp(prevSkyboxExposure, targetSkyboxExposure, transitionPercent);
            RenderSettings.fogColor = Color.Lerp(prevFogColor, targetFogColor, transitionPercent);
            transitionPercent += Time.deltaTime;
            yield return null;
        }
        
        
        RenderSettings.ambientIntensity = targetAmbientIntensity;
        RenderSettings.sun.intensity = targetSunlightIntensity;
    }


    public void OnCombatStart() {
        curLevel = 0;
        combatStartTime = Time.timeSinceLevelLoad;
        CalculateDifficulty();
    }

    public void OnCombatEnd(bool isReal) {
        infoText.text = "";
    }

}
