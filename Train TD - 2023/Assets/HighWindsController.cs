using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class HighWindsController : MonoBehaviour {

    public static HighWindsController s;

    private void Awake() {
        s = this;
    }

    // Start is called before the first frame update
    void Start() {
        GetComponentInParent<BiomeEffectsController>().OnBiomeEffectsChanged += OnBiomeEffectsChanged;
    }


    public bool highWindsPresent = false;
    public bool currentlyHighWinds = false;
    public float highWindsTimer = 0;

    public GameObject regularWinds;
    public GameObject highWinds;
    public WindZone trainForwardWind;
    void OnBiomeEffectsChanged() {
        highWindsPresent = BiomeEffectsController.s.GetCurrentEffects().highWinds;
        
        DeactivateHighWinds();
        if (highWindsPresent) {
            highWindsTimer = Mathf.Pow(Random.Range(3,15),2); // between 9 seconds and 3.75 minutes
        }
        regularWinds.SetActive(highWindsPresent);
    }

    // Update is called once per frame
    void Update() {
        if (highWindsPresent) {
            if (EnemyWavesController.s.AnyEnemyIsPresent()) {
                highWindsTimer -= Time.deltaTime;
            } else {
                DeactivateHighWinds();
            }

            if (highWindsTimer <= 0) {
                if (!currentlyHighWinds) {
                    ActivateHighWinds();
                    highWindsTimer = Random.Range(5, 10);
                } else {
                    DeactivateHighWinds();
                    highWindsTimer = Random.Range(10, 15);
                }
            }

            regularWinds.transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(0);
            regularWinds.transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(0);
            highWinds.transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(0);
        }
        
        trainForwardWind.transform.position =  PathAndTerrainGenerator.s.GetPointOnActivePath(0);
        trainForwardWind.transform.rotation =  PathAndTerrainGenerator.s.GetRotationOnActivePath(0);
        trainForwardWind.windMain = -LevelReferences.s.speed / 7;
        
        if (LevelReferences.s.speed > 4) {
            regularWinds.SetActive(true);
            regularWinds.transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(0);
            regularWinds.transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(0);
            currentlyHighWinds = true;
            
        } else {
            if (!highWindsPresent) {
                regularWinds.SetActive(false);
                currentlyHighWinds = false;
            }
        }
    }


    public bool IsStopped() {
        return LevelReferences.s.speed < 1;
    }

    void ActivateHighWinds() {
        currentlyHighWinds = true;
        highWinds.SetActive(currentlyHighWinds);
        //highWindsUI.SetActive(currentlyHighWinds);
    }

    void DeactivateHighWinds() {
        currentlyHighWinds = false;
        highWinds.SetActive(currentlyHighWinds);
        //highWindsUI.SetActive(currentlyHighWinds);
    }
}
