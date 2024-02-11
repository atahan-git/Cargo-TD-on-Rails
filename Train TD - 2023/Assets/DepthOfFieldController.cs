using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class DepthOfFieldController : MonoBehaviour
{
    public static DepthOfFieldController s;

    private Volume _volume;
    private DepthOfField _depthOfField;
    private void Awake() {
        s = this;
        _volume = GetComponent<Volume>();
        _volume.profile.TryGet(out _depthOfField);
    }


    private Vector2 curDepthOfField;
    public Vector2 minDepthOfField = new Vector2(8, 70);
    public Vector2 maxDepthOfField = new Vector2(7.5f, 15);

    private bool isEnabled;

    private float lastAngle;

    public bool madeACameraJump = false;
    private void Update() {
        /*if (isEnabled) {
            var angle = MainCameraReference.s.cam.transform.forward.y;
            print(angle);

            var percent = angle / -0.7f;

            percent = Mathf.Clamp01(percent);

            var target = Vector2.Lerp(minDepthOfField, maxDepthOfField, percent);

            curDepthOfField = Vector2.Lerp(curDepthOfField, target, 1f*Time.deltaTime);
            
            if (madeACameraJump) {
                madeACameraJump = false;
                curDepthOfField = target;
            } 
        }*/
    }

    public void SetDepthOfField(bool _isEnabled) {
        isEnabled = _isEnabled;
        _depthOfField.active = isEnabled;
    }
}
