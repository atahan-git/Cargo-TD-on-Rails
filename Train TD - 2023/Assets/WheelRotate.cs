using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelRotate : MonoBehaviour {
    public Vector3 rotationVector = new Vector3(1, 0, 0);
    private float radius;
    private float multiplier; // magic number that gives us degrees to rotate given linear speed
    // eg
    // C = 2 pi r
    // for C = 10 and speed = 10 angle = 360
    // for C = 20 and speed = 10 angle = 180
    // so angle = speed/C * 360
    // angle = speed/(2pi r) * 360
    // angle = speed * (180/(pi r))
    private void Start() {
        radius = GetComponent<SphereCollider>().radius;
        multiplier = (180 / (Mathf.PI * radius))*2 ;//double it because for some reason it is too slow
    }

    private Vector3 lastPos;
    void Update() {
        var speed = (transform.position - lastPos).magnitude / Time.deltaTime;
        multiplier = (180 / (Mathf.PI * radius))*2;//double it because for some reason it is too slow
        transform.Rotate(rotationVector * speed * multiplier * Time.deltaTime) ;
        lastPos = transform.position;
    }
}
