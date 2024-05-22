using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemRewardOnRoad : MonoBehaviour, IShowOnDistanceRadar {
    private float myDistance = 0;

    public Transform snapTarget;

    private void Start() {
        SetUp(PathSelectorController.s.nextSegmentChangeDistance);
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
    public void OnCollisionEnter(Collision collision) {
        if (!awardGiven && collision.collider.GetComponentInParent<Train>()) {
            awardGiven = true;
            GiveReward();
            CameraController.s.UnSnap();
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!awardGiven && other.GetComponentInParent<Train>()) {
            DirectControlMaster.s.DisableDirectControl();
            CameraController.s.SnapToTransform(Train.s.carts[0].transform);
        }
    }

    void GiveReward() {
        VisualEffectsController.s.SmartInstantiate(toSpawnOnDeath, transform.position, transform.rotation, VisualEffectsController.EffectPriority.Always);
        StopAndPick3RewardUIController.s.ShowGemReward();
    }

    public Sprite radarIcon;

    public bool IsTrain() {
        return false;
    }

    public float GetDistance() {
        return myDistance-SpeedController.s.currentDistance;
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