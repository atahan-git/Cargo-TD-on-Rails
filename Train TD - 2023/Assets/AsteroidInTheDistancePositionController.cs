using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidInTheDistancePositionController : MonoBehaviour {
    public static AsteroidInTheDistancePositionController s;

    private void Awake() {
        s = this;
    }

    public bool centerMode = false;
    private Vector3 smoothDamp;
    void Update() {
        if (!centerMode) {
            var curDepth = PathAndTerrainGenerator.s.currentPathTree.myDepth;
            var missionDepth = MapController.s.currentMap.bossDepth;

            var depthPercent = ((float)curDepth) / missionDepth;
            depthPercent = Mathf.Clamp01(depthPercent);



            var depthCloseness = depthPercent.Remap(0, 1, 0, 100);
            var actCloseness = (DataSaver.s.GetCurrentSave().currentRun.currentAct - 1) * 50;

            var asteroidGetCloserAmount = Mathf.Clamp(depthCloseness + actCloseness, 0, 300);
            var actualDistance = (300 - asteroidGetCloserAmount);
            actualDistance *= 0.5f;
            actualDistance += 75;

            var targetPos = Train.s.trainMiddle.position + Train.s.GetTrainForward() * actualDistance;
            transform.position += Train.s.GetTrainForward() * LevelReferences.s.speed * Time.deltaTime;
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref smoothDamp, 10, 50f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(-Train.s.GetTrainForward(), Vector3.up), 20*Time.deltaTime);
        } else {
            transform.position = PathAndTerrainGenerator.CircleTerrainCenter();
        }
    }
}
