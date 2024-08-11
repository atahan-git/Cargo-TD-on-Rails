using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemRewardOnRoad : MonoBehaviour, IShowOnDistanceRadar {
    public float myDistance = 0;

    public bool autoStart = true;

    public bool isBigGem = false;

    private void Start() {
        if (autoStart) {
            SetUp(PathSelectorController.s.nextSegmentChangeDistance);
        }
    }

    public void SetUp(float distance) {
        myDistance = distance;
        Update();
        DistanceAndEnemyRadarController.s.RegisterUnit(this);
    }


    private void Update() {
        transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(myDistance - SpeedController.s.currentDistance);
        transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(myDistance - SpeedController.s.currentDistance);

    }

    public GameObject toSpawnOnDeath;
    public bool awardGiven = false;
    public void OnTriggerEnterCollision(Collider other) {
        if (!awardGiven && other.GetComponentInParent<Train>()) {
            awardGiven = true;
            GiveReward();
            CameraController.s.UnSnap();
            Destroy(gameObject);
        }
    }

    private bool pulledOut = false;
    public void OnTriggerEnterCameraSwitch(Collider other) {
        if (!awardGiven &&!pulledOut&& other.GetComponentInParent<Train>()) {
            pulledOut = true;
            DirectControlMaster.s.DisableDirectControl();
            CameraController.s.SnapToTransform(Train.s.carts[0].transform);
            LevelReferences.s.speed = 5;
            //Train.s.GetComponentInChildren<EngineModule>().currentPressure = 0.5f;
        }
    }

    void GiveReward() {
        var deathEffect = VisualEffectsController.s.SmartInstantiate(toSpawnOnDeath, transform.position, transform.rotation, VisualEffectsController.EffectPriority.Always);
        StopAndPick3RewardUIController.s.ShowGemReward( isBigGem);
    }

    public Sprite radarIcon;

    public bool IsTrain() {
        return false;
    }

    public float GetDistance() {
        return myDistance;
    }

    public Sprite GetIcon() {
        return radarIcon;
    }

    public bool isLeftUnit() {
        return true;
    }

    private void OnDestroy() {
        DistanceAndEnemyRadarController.s.RemoveUnit(this);
    }
}