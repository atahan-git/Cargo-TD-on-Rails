using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class SizeBasedOnZoomLevel : MonoBehaviour {

    
    [InfoBox("Every 1 unit of zoom change will result in multiplier amount of scale change")]
    public float zoomSizeChangeMultiplier = -0.5f;


    public bool pulseSize = false;
    
    [ShowIf("pulseSize")]
    public float speed = 1f;

    public float lerp = 0.5f;
    
    // 2 >> 0.5
    // 0 >> 0
    // -2 >> -0.5

    private float curScale = 1;


    private float curTime = 0.5f;
    void Update() {
        var wantedScale = (1 + CameraController.s.realZoom * zoomSizeChangeMultiplier);
        curScale = Mathf.Lerp(curScale, wantedScale, lerp * Time.deltaTime);
        var realScale = curScale;
        if (pulseSize) {
            realScale *= LevelReferences.s.selectMarkerPulseCurve.Evaluate(curTime);
            curTime += Time.deltaTime * speed;
        }

        transform.localScale = Vector3.one * realScale;
    }
}
