using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DisableCameraZoomWhenInsideRect : MonoBehaviour {
    private RectTransform rect;
    void Start() {
        rect = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update() {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        var insideRect = RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, OverlayCamsReference.s.uiCam);

        CameraController.s.canZoom = !insideRect;
    }


    private void OnDisable() {
        CameraController.s.canZoom = true;
    }
}
