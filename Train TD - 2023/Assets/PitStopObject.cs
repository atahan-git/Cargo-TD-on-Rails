using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PitStopObject : MonoBehaviour, IShowOnDistanceRadar {

    public float myDistance;
    public void SetUp(float distance) {
        myDistance = distance;
        awardGiven = false;
        DistanceAndEnemyRadarController.s.RegisterUnit(this);
    }

    public bool awardGiven = false;
    private void Update() {
        var relativeDistance = myDistance - SpeedController.s.currentDistance;
        transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(relativeDistance);
        transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(relativeDistance);

        if (!awardGiven) {
            if (SpeedController.s.currentDistance > myDistance) {
                awardGiven = true;
                StopAndPick3RewardUIController.s.ShowCartReward();
                DistanceAndEnemyRadarController.s.RemoveUnit(this);
            }
        }

        if (SpeedController.s.currentDistance > myDistance + 100) {
            Destroy(gameObject);
        }
    }

    public Sprite radarIcon;

    public bool IsTrain() {
        return false;
    }

    public float GetDistance() {
        return myDistance - SpeedController.s.currentDistance;
    }

    public Sprite GetIcon() {
        return radarIcon;
    }

    public bool isLeftUnit() {
        return true;
    }
}
