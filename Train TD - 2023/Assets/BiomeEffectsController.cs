using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeEffectsController : MonoBehaviour {
    public static BiomeEffectsController s;

    private void Awake() {
        s = this;
    }

    public BiomeEffects currentEffects = new BiomeEffects();
    
    public BiomeEffects GetCurrentEffects() {
        return currentEffects;
    }

    public class BiomeEffects {
        public float gatlinificationGunsMaxEffectMultiplier = 1f; // smaller is better. ie 92% max reduction + 0.5 multiplier = 96% max reduction
        public float gatlinificationIncreaseRate = 1f;
        public float burnDecayMultiplier = 1f;
        public int maxBurnTierChange = 0;
        public float repairTimeMultiplier = 1f;
        public bool highWinds = false;
    }

    public Action OnBiomeEffectsChanged;
    public void ApplyBiomeEffects(Biome biome) {
        var index = biome.biomeIndex;

        currentEffects = new BiomeEffects();

        switch (index) {
            case 0: // grasslands
                // nothing
            break;
            case 1: // drylands
                currentEffects.gatlinificationGunsMaxEffectMultiplier = 2f;
                currentEffects.burnDecayMultiplier = 0.5f;
                currentEffects.repairTimeMultiplier = 2f;
                currentEffects.maxBurnTierChange = 1;
                break;
            case 2: // snowlands
                currentEffects.gatlinificationGunsMaxEffectMultiplier = 0.75f;
                currentEffects.burnDecayMultiplier = 2;
                currentEffects.gatlinificationIncreaseRate = 0.5f;
                currentEffects.highWinds = true;
                currentEffects.maxBurnTierChange = -1;
                break;
            default:
                Debug.LogError("Biome doesn't have biome effects set!");
                break;
        }
        
        
        OnBiomeEffectsChanged?.Invoke();
    }

}
