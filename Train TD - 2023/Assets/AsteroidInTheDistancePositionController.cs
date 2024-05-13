using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidInTheDistancePositionController : MonoBehaviour {
    private Vector3 smoothDamp;
    void Update() {
        var curDepth = PathAndTerrainGenerator.s.currentPathTree.myDepth;
        var missionDepth = MapController.s.currentMap.bossDepth;

        var depthPercent = ((float)curDepth) / missionDepth;
        depthPercent = Mathf.Clamp01(depthPercent);
        
        

        var depthCloseness = depthPercent.Remap(0, 1, 0, 100);
        var actCloseness = (DataSaver.s.GetCurrentSave().currentRun.currentAct-1)*100;
        
        var asteroidGetCloserAmount = Mathf.Clamp(depthCloseness+actCloseness, 0, 300);
        var actualDistance = (300 - asteroidGetCloserAmount);
        actualDistance *= 0.5f;
        actualDistance += 20;

        var targetPos = Train.s.GetTrainForward() * actualDistance;
        transform.position = Vector3.SmoothDamp(transform.position,targetPos,ref smoothDamp, 20,0.05f);
    }
}
