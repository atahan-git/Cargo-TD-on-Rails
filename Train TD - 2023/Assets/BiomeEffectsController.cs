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

    public enum BiomeIdentifier {
        grasslands, drylands, snowlands, purplelands, darklands
    }
    public class BiomeEffects {
        public BiomeIdentifier currentBiome = BiomeIdentifier.grasslands;
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

        var currentAct = DataSaver.s.GetCurrentSave().currentRun.currentAct;

        switch (currentAct) {
            case 1: {
                switch (index) {
                    case 0: // grasslands
                        currentEffects.currentBiome = BiomeIdentifier.grasslands;
                        break;
                    case 1: // drylands
                        currentEffects.currentBiome = BiomeIdentifier.drylands;
                        currentEffects.gatlinificationGunsMaxEffectMultiplier = 2f;
                        currentEffects.burnDecayMultiplier = 0.5f;
                        currentEffects.repairTimeMultiplier = 2f;
                        currentEffects.maxBurnTierChange = 1;
                        break;
                    case 2: // snowlands
                        currentEffects.currentBiome = BiomeIdentifier.snowlands;
                        currentEffects.gatlinificationGunsMaxEffectMultiplier = 0.75f;
                        currentEffects.burnDecayMultiplier = 2;
                        currentEffects.gatlinificationIncreaseRate = 0.5f;
                        currentEffects.highWinds = true;
                        currentEffects.maxBurnTierChange = -1;
                        break;
                    default:
                        Debug.LogError("Biome doesn't have biome effects set! {index}");
                        break;
                }
            }
                break;
            case 2: {
                switch (index) {
                    case 0: // purple grasslands
                        currentEffects.currentBiome = BiomeIdentifier.purplelands;
                        break;
                    default:
                        Debug.LogError($"Biome doesn't have biome effects set! {index}");
                        break;
                }
            }
                break;
            case 3: {
                switch (index) {
                    case 0: // purple grasslands
                        currentEffects.currentBiome = BiomeIdentifier.darklands;
                        break;
                    default:
                        Debug.LogError($"Biome doesn't have biome effects set! {index}");
                        break;
                }
            }
                break;
            default:
                Debug.LogError($"Unknown act! {currentAct}");
                break;
        }
        
        
        
        OnBiomeEffectsChanged?.Invoke();
    }

}
