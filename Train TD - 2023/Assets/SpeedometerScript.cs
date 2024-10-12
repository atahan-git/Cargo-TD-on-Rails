using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class SpeedometerScript : MonoBehaviour {

    public float minSpeed = 0.2f;
    public float maxSpeed = 10;
    public RectTransform gauge;
    public float minRotation = -90;
    public float maxRotation = 45;

    [Button]
    public void SetSpeed(float speed) {
        gauge.rotation = Quaternion.Euler(0,0,speed.Remap(minSpeed,maxSpeed,minRotation,maxRotation));
    }

    [Button]
    public void Reset() {
        gauge.rotation = Quaternion.identity;
    }
}
