using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightShellController : MonoBehaviour {

    [Tooltip("Light 0 will be used as the prefab to copy over to the others when increasing light count")]
    public List<Light> lights;
    public float lightYOffset = 1.22f;
    public Transform[] outerBones;
    public Vector2 outerStartEndOffsets;
    public GameObject outerShell;
    public Transform[] innerBones;
    public Vector2 innerStartEndOffsets;
    public GameObject innerShell;
    public float yOffset = 0.658f;

    public ParticleSystem shellParticles;

    public bool lightsActive = true;
    public bool engineParticlesActive = true;
    public WarpGlow[] engineParticles;

    private Vector3 boneScale;

    private void Start() {
        boneScale = innerBones[0].transform.localScale;
        curLightAmount = 0;
        Train.s.onTrainCartsChanged.AddListener(UpdateLightCount);
        Train.s.onTrainCartsChanged.AddListener(UpdateEngineParticles);
        lightsActive = true;
        engineParticlesActive = true;
    }

    void UpdateEngineParticles() {
        engineParticles = Train.s.GetComponentsInChildren<WarpGlow>();
    }


    void UpdateLightCount() {
        var trainLength = Train.s.GetTrainLength();

        var lightCount = Mathf.CeilToInt((trainLength + 1) / 1.5f) + 1;

        lightCount = Mathf.Clamp(lightCount, 1, 8);

        if (lightCount != lights.Count) {
            while (lights.Count > lightCount  ) {
                var index = lights.Count - 1;
                var decommissioned = lights[index];
                lights.RemoveAt(index);
                decommissioned.GetComponent<SmartDestroy>().Engage();
            }
            
            while (lights.Count < lightCount) {
                var newLight = Instantiate(lights[0].gameObject, lights[0].transform.parent).GetComponent<Light>();
                newLight.intensity = 0;
                newLight.transform.position = lights[^1].transform.position;
                lights.Add(newLight);
            }
        }
    }

    public float curLightAmount = 1f;
    public float lightMultiplier = 0.8f;

    void LateUpdate() {
        if (Train.s.carts.Count > 0) {
            var trainEngine = Train.s.carts[0];
            transform.position = trainEngine.transform.position;
        }

        var targetLightAmount = 0f;
        if (CrystalsAndWarpController.s.warpProgress >= 2) {
            targetLightAmount = 1;
        }
        if (!PlayStateMaster.s.isCombatInProgress()) {
            targetLightAmount = 0;
        }

        targetLightAmount = Mathf.Clamp01(targetLightAmount);
        
        curLightAmount =  Mathf.Lerp(curLightAmount, targetLightAmount, Time.deltaTime);

        var minDistance = float.MinValue;
        var maxDistance = float.MaxValue;

        if (PathSelectorController.s.trainStationStart.activeSelf) {
            minDistance = -SpeedController.s.currentDistance + (PathSelectorController.s.trainStationStart.GetComponent<TrainStation>().stationDistance + 8.5f);
        }
        
        if (PathSelectorController.s.trainStationEnd.activeSelf) {
            maxDistance = -SpeedController.s.currentDistance + (PathSelectorController.s.trainStationEnd.GetComponent<TrainStation>().stationDistance - 9f);
        }

        {
            var spanLength = Train.s.GetTrainLength() + 1f;
            var startDist = spanLength / 2f;

            var stepDistance = spanLength / (lights.Count-1);
            for (int i = 0; i < lights.Count; i++) {
                var realDist = startDist - i * stepDistance;
                var dist = Mathf.Clamp(realDist, minDistance, maxDistance);
                lights[i].transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(realDist) + Vector3.up*lightYOffset;
                
                /*var scaleLerp = Mathf.Abs(realDist - dist) / 10f;
                scaleLerp = 1f-Mathf.Clamp01(scaleLerp);*/
                var scaleLerp = Mathf.Min(Mathf.Abs(realDist - minDistance),Mathf.Abs(realDist - maxDistance))/2f;
                scaleLerp = Mathf.Clamp01(scaleLerp);

                var inLegalZone = realDist > minDistance && realDist < maxDistance;

                lights[i].intensity = Mathf.Lerp(lights[i].intensity, curLightAmount * scaleLerp * lightMultiplier, 1f*Time.deltaTime);
                lights[i].enabled = inLegalZone && lightsActive;
            }
        }
        
        
        {
            var spanLength = Train.s.GetTrainLength();
            var startDist = spanLength / 2f;
            startDist += innerStartEndOffsets.x;
            spanLength += innerStartEndOffsets.x + innerStartEndOffsets.y;
            var stepDistance = spanLength / (innerBones.Length-1);
            for (int i = 0; i < innerBones.Length; i++) {
                var realDist = startDist - i * stepDistance;
                var dist = Mathf.Clamp(realDist, minDistance, maxDistance);
                innerBones[i].transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(dist) + Vector3.up*yOffset;
                innerBones[i].transform.rotation = Quaternion.Euler(180, 0, 0) * Quaternion.Inverse( PathAndTerrainGenerator.s.GetRotationOnActivePath(dist));

                var scaleLerp = Mathf.Abs(realDist - dist) / 5f;
                scaleLerp = 1f-Mathf.Clamp01(scaleLerp);
                
                innerBones[i].transform.localScale = boneScale*scaleLerp*curLightAmount;
            }
        }

        {
            var spanLength = Train.s.GetTrainLength();
            var startDist = spanLength / 2f;
            startDist += outerStartEndOffsets.x;
            spanLength += outerStartEndOffsets.x + outerStartEndOffsets.y;
            var stepDistance = spanLength / (outerBones.Length - 1);
            for (int i = 0; i < outerBones.Length; i++) {
                var realDist = startDist - i * stepDistance;
                var dist = Mathf.Clamp(realDist, minDistance, maxDistance);
                outerBones[i].transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(dist) + Vector3.up * yOffset;
                outerBones[i].transform.rotation = Quaternion.Euler(180, 0, 0) * Quaternion.Inverse(PathAndTerrainGenerator.s.GetRotationOnActivePath(dist));
                
                var scaleLerp = Mathf.Abs(realDist - dist) / 5f;
                scaleLerp = 1f-Mathf.Clamp01(scaleLerp);
                
                outerBones[i].transform.localScale = boneScale*scaleLerp*curLightAmount;
            }
        }
        

        var insideStationScaling = 1f - Mathf.Clamp01(Mathf.Abs(Mathf.Clamp(0, minDistance, maxDistance)/5f));


        if (curLightAmount < 0.05f) {
            if (lightsActive) {
                for (int i = 0; i < lights.Count; i++) {
                    lights[i].enabled = false;
                }
                shellParticles.Stop();
                
                innerShell.SetActive(false);
                outerShell.SetActive(false);

                lightsActive = false;
            }


        } else {
            if (!lightsActive) {
                shellParticles.Play();
                
                innerShell.SetActive(true);
                outerShell.SetActive(true);

                lightsActive = true;
            }

            var forceOverLifetime = shellParticles.forceOverLifetime;
            forceOverLifetime.z = LevelReferences.s.speed*insideStationScaling;
        }


        var engineParticleAmount = CrystalsAndWarpController.s.warpProgress;
        engineParticleAmount = Mathf.Clamp01(engineParticleAmount);
        
        if (engineParticleAmount < 0.05f) {
            if (engineParticlesActive) {
                engineParticlesActive = false;
                for (int i = 0; i < engineParticles.Length; i++) {
                    if(engineParticles[i] != null)
                        engineParticles[i].SetGlowState(engineParticlesActive);
                }
                
                
            }


        } else {
            if (!engineParticlesActive) {
                engineParticlesActive = true;
                for (int i = 0; i < engineParticles.Length; i++) {
                    engineParticles[i].SetGlowState(engineParticlesActive);
                }
            }
            
            for (int i = 0; i < engineParticles.Length; i++) {
                engineParticles[i].SetScale(engineParticleAmount);
            }
        }
    }
}
