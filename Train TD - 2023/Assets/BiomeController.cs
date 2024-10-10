using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class BiomeController : MonoBehaviour {
    public static BiomeController s;

    private void Awake() {
        s = this;
    }
    [SerializeField] private Biome currentBiome;
    public Light sun;

    public BiomeScriptable debugBiomeOverride;

    public ActBiomesHolder[] actBiomes;

    public int GetRandomBiomeVariantForCurrentAct() {
        var act = actBiomes[GetAct()];

        var biomeChances = new List<NumberWithWeights>();
        for (int i = 0; i < act.biomes.Length; i++) {
            biomeChances.Add(new NumberWithWeights(i, act.biomes[i].myBiome.biomeSpawnWeight));
        }

        var variant = NumberWithWeights.WeightedRandomRoll(biomeChances.ToArray());
        return variant;
    }


    public int GetAct() {
        var targetBiome = DataSaver.s.GetCurrentSave().currentRun.currentAct-1;
        if (targetBiome < 0 || targetBiome > actBiomes.Length) {
            Debug.LogError($"Illegal biome {targetBiome}");
            targetBiome = 0;
        }

        return targetBiome;
    }

    public void SetDefaultBiome() {
        var act = actBiomes[GetAct()];
        SetBiome(act.biomes[0].myBiome);
    }

    public void SetBiome(int variant) {
        SetBiome(actBiomes[GetAct()].biomes[variant].myBiome);
    }
    
    

    public void SetParticularBiome(int act, int variant, bool repopulateTerrain = true) {
        SetBiome(actBiomes[act].biomes[0].myBiome, repopulateTerrain);
    }
    
    public void SetBiome(Biome source, bool repopulateTerrain = true){
        if (!Application.isEditor) {
            _SetBiome(source, repopulateTerrain);
        }
    
        if (debugBiomeOverride == null) {
            _SetBiome(source, repopulateTerrain);
        } else {
            _SetBiome(debugBiomeOverride.myBiome, repopulateTerrain);
        }
    }
    
    void _SetBiome(Biome source, bool repopulateTerrain = true) {
        Debug.Log($"Setting biome {source.debugName}");
        currentBiome = source;
        
        currentBiome.sun.ApplyToSun(sun);
        currentBiome.skybox.SetActiveSkybox(sun, null);
        RenderSettings.ambientIntensity = currentBiome.skyboxLightIntensity;
        //PPController.s.SetPostExposure(currentBiome.postExposure);
        //RenderSettings.subtractiveShadowColor = currentBiome.shadowColor;

        if (repopulateTerrain) {
            var repopulated = PathAndTerrainGenerator.s.terrainPool.RePopulateWithNewObject(currentBiome.terrainPrefab);
            if(repopulated)
                PathAndTerrainGenerator.s.myTerrains.Clear();
        }
        
        BiomeEffectsController.s.ApplyBiomeEffects(currentBiome);
    }


    public Biome GetBiomeVariant(int variant) {
        return actBiomes[GetAct()].biomes[variant].myBiome;
    }
    public Biome GetCurrentBiome() {
        return currentBiome;
    }
    
    [Button]
    void DebugApplySunAndSkyboxEditor(BiomeScriptable target) {
        _SetBiome(target.myBiome);
        PathAndTerrainGenerator.s.DebugReDrawTerrainAroundCenter();
    }

}

[Serializable]
public class ActBiomesHolder {
    public BiomeScriptable[] biomes;
}

