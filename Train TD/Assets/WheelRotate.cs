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
        multiplier = 180 / (Mathf.PI * radius);
    }

    void Update()
    {
        transform.Rotate(rotationVector * LevelReferences.s.speed * multiplier * Time.deltaTime) ;
    }
}
