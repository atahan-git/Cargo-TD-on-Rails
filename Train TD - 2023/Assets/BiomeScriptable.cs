using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;



[CreateAssetMenu()]
public class BiomeScriptable : ScriptableObject {
    public Biome myBiome;

    private void OnValidate() {
        myBiome.debugName = this.name;
    }
}


[System.Serializable]
public class Biome {
    public string debugName;

    public float biomeSpawnWeight = 1;
    
    [Header("Prefabs")]
    public GameObject terrainPrefab;
    [SerializeField]
    GameObject dioramaPrefab;
    public GameObject GetDioramaPrefab() {
        return dioramaPrefab;
    }

    [Header("Light Settings")] 
    public SunSettings sun;
    public SkyboxParametersScriptable skybox;
    public float skyboxLightIntensity = 1.56f;

    [Header("Terrain Gen")] 
    public TerrainGenerationSettings genSettings;
}


[Serializable]
public class SunSettings {
    public Quaternion rotation;
    public float colorTemperature;
    public float intensity;
    
    [Button]
    public void EditorGetLightSettings(Light source) {
        rotation = source.transform.rotation;
        colorTemperature = source.colorTemperature;
        intensity = source.intensity;
    }

    public void ApplyToSun(Light sun) {
        sun.transform.rotation = rotation;
        sun.colorTemperature = colorTemperature;
        sun.intensity = intensity;
    }
}

[Serializable]
public class TerrainGenerationSettings {
    public GrassData grass0;
    public GrassData grass1;

    public TreeData[] treeDatas;
    
    [Serializable]
    public struct GrassData {
        public float grassFrequency;
        public float grassThreshold;
        public int grassMaxDensity;
    }
    
    [Serializable]
    public struct TreeData {
        public float treeFrequency;
        public float treeThreshold;
        public float treeMaxThreshold;
        public float treeChanceAtMaxDensity;
        public Vector2 sizeScale;
        public float minDensity;
    }
}