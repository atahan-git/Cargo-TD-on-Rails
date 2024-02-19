using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainStation : MonoBehaviour {
    public float stationDistance;

    public bool autoDisable = false;
    private void Start() {
        //startPos = transform.position;
    }

    public void Update() {
        if (autoDisable && Mathf.Abs(stationDistance- SpeedController.s.currentDistance) > 250) {
            gameObject.SetActive(false);
        }
        
        transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(stationDistance-SpeedController.s.currentDistance) ;
        transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(stationDistance-SpeedController.s.currentDistance) /** Quaternion.Euler(0,180,0)*/;
    }
}
