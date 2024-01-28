using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainStation : MonoBehaviour {
    public float stationDistance;
    private void Start() {
        //startPos = transform.position;
    }

    void Update() {
        /*if(!PlayStateMaster.s.isCombatStarted())
            return;*/
        if (stationDistance - SpeedController.s.currentDistance > 250) {
            gameObject.SetActive(false);
        }
        
        transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(stationDistance-SpeedController.s.currentDistance) ;
        transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(stationDistance-SpeedController.s.currentDistance) /** Quaternion.Euler(0,180,0)*/;
    }
}
