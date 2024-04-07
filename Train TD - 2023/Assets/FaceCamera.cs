using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class FaceCamera : MonoBehaviour {
	private Transform camera;
	void Start() {
	    camera = MainCameraReference.s.cam.transform;
    }

    // Update is called once per frame
    void Update() {
	    transform.LookAt(camera);
    }

    [Button]
    void DebugFaceCam() {
	    transform.LookAt(Camera.main.transform);
    }
}
