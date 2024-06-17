using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class TreeBillboardFaceCamera : MonoBehaviour {
    private Transform camera;
    void Start() {
        camera = MainCameraReference.s.cam.transform;
    }

    // Update is called once per frame
    void Update() {
        transform.rotation = camera.rotation;
    }


    [Button]
    void EditorLookAtCam() {
        transform.rotation = Camera.main.transform.rotation;
    }
}