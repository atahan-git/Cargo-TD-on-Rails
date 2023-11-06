using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainGemBridge : MonoBehaviour, IResetState, IActiveDuringCombat
{
    public List<GameObject> extraPrefabToSpawnOnAffected = new List<GameObject>();
    public GameObject uranium;

    private float uraniumDelay = 10f;
    public float uraniumDelayReduction = 0;

    public float curDelay = 1f;
    public float curUraniumDelay = 0;

    
    public List<GameObject> prefabToSpawnWhenSpeedBoostActivates = new List<GameObject>();
    
    void Update() {
        curDelay -= Time.deltaTime;
        curUraniumDelay -= Time.deltaTime;

        if (curDelay <= 0) {
            SpawnGemEffect();
            curDelay = 2f;
            if (SpeedController.s.isBoosting) {
                curDelay = extraPrefabToSpawnOnAffected.Count*0.2f;
            }
        }

        if (uranium != null) {
            if (curUraniumDelay <= 0) {
                curUraniumDelay = Mathf.Max(1,uraniumDelay - uraniumDelayReduction); // min delay is 1 sec
                Instantiate(uranium, transform);
                
                if (SpeedController.s.isBoosting) {
                    curUraniumDelay = 1f;
                }
            }
        }
    }
    
    void SpawnGemEffect() {
        StartCoroutine(_SpawnGemEffect());
    }

    IEnumerator _SpawnGemEffect() {
        foreach (var prefab in extraPrefabToSpawnOnAffected) {
            Instantiate(prefab, transform);

            yield return new WaitForSeconds(0.2f);
        }
    }

    void SpawnSpeedBoostEffect() {
        StartCoroutine(_SpawnSpeedBoostEffect());
    }
    
    IEnumerator _SpawnSpeedBoostEffect() {
        foreach (var prefab in prefabToSpawnWhenSpeedBoostActivates) {
            Instantiate(prefab, transform);

            yield return new WaitForSeconds(0.2f);
        }
    }

    public void ResetState(int level) {
        extraPrefabToSpawnOnAffected.Clear();
        prefabToSpawnWhenSpeedBoostActivates.Clear();
        uranium = null;
        uraniumDelayReduction = 0;
    }

    public void ActivateForCombat() {
        this.enabled = true;
        curDelay = 10;
        curUraniumDelay = 15;
    }

    public void Disable() {
        this.enabled = false;
    }

    private void Start() {
        SpeedController.s.OnSpeedBoostActivated.AddListener(SpawnSpeedBoostEffect);
    }

    private void OnDestroy() {
        SpeedController.s.OnSpeedBoostActivated.RemoveListener(SpawnSpeedBoostEffect);
    }
}
