using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PitStopController : MonoBehaviour {
    public static PitStopController s;

    private void Awake() {
        s = this;
    }

    public GameObject pitStopPrefab;
    public PitStopObject curPitStopObject;

    public void MakePitStop(float segmentStartDistance, float segmentLength) {
        var distance = segmentStartDistance + Random.Range(segmentLength *2f/ 8f, segmentLength*6f/8f);
        
        var playerDistance = SpeedController.s.currentDistance;
        curPitStopObject = Instantiate(pitStopPrefab, Vector3.forward * (distance - playerDistance), Quaternion.identity).GetComponent<PitStopObject>();
        curPitStopObject.transform.SetParent(transform);
        curPitStopObject.SetUp(distance);
    }
}
