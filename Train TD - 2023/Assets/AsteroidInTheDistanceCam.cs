using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidInTheDistanceCam : MonoBehaviour {
    public static AsteroidInTheDistanceCam s;

    [HideInInspector]
    public Camera camera;

    private Transform mainCam;
    private void Awake() {
        s = this;
        camera = GetComponent<Camera>();
    }

    private void Start() {
        mainCam = MainCameraReference.s.cam.transform;
        transform.rotation = mainCam.rotation;
        CameraController.s.AfterCameraPosUpdate.AddListener(UpdateCam);
    }

    void UpdateCam() {
        transform.rotation = mainCam.rotation;
    }
}
