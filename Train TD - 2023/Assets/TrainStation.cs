using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainStation : MonoBehaviour {
    public Vector3 startPos;
    private void Start() {
        //startPos = transform.position;
    }

    void Update() {
        transform.position = startPos + SpeedController.s.currentDistance * Vector3.back;
    }
}
