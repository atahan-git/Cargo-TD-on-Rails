using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class OverlayCamsReference : MonoBehaviour {
    public static OverlayCamsReference s;

    private void Awake() {
        s = this;
    }

    public Camera uiCam;

    
    public const float planeDistance = 1.5f;

    private void Start() {
        var camData = MainCameraReference.s.cam.GetUniversalAdditionalCameraData();
        camData.cameraStack.Clear();
        camData.cameraStack.Add(uiCam);
    }
}
