using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class CameraShakeController : MonoBehaviour {
    public static CameraShakeController s;

    private void Awake() {
        s = this;
    }

    private bool isShaking = false;
    public float horizontalPush;

    public Transform cameraShakeDummy;

    public List<ShakeWave> activeShakes = new List<ShakeWave>();

    public class ShakeWave {
        public float curMagnitude;
        public float magnitudeDecay;
        public float verticalMagnitude;
        public float verticalMagnitudeDecay;
    }

    public float overallShakeAmount = 1f;


    public void ShakeCamera(float duration, float magnitude, float verticalPushMagnitude = 0f, float verticalPushDuration = 1f, bool doHorizontalPush = false) {
		
        /*var bigDur = Mathf.Max(curDuration, duration);
        var smallDur = Mathf.Min(curDuration, duration);
        curDuration = bigDur + (Mathf.Log(smallDur+1)*2);
        
        var bigMag = Mathf.Max(curMagnitude, magnitude);
        var smallMag = Mathf.Min(curMagnitude, magnitude);
        curMagnitude = bigMag + (Mathf.Log(smallMag+1)*2);*/
		
        /*curDuration += duration;
        curMagnitude += magnitude;*/
        
        if(doHorizontalPush)
            horizontalPush = Random.Range(-1f, 1f);

        var newShake = new ShakeWave() {
            curMagnitude =  magnitude,
            magnitudeDecay = magnitude/duration,
            verticalMagnitude =  verticalPushMagnitude,
            verticalMagnitudeDecay = verticalPushMagnitude/verticalPushDuration
        };

        activeShakes.Add(newShake);
        
        if (!isShaking) {
            StartCoroutine(Shake());
        }
    }
    
    public bool rotationalShake = false;
	
    IEnumerator Shake() {
        isShaking = true;
        //var originalLocalPosition = PlayerReference.s.playerHead.localPosition;
        while (activeShakes.Count > 0) {
            var curMagnitude = 0f;
            var verticalPushMagnitude = 0f;
            for (int i = activeShakes.Count-1; i >= 0; i--) {
                curMagnitude = Mathf.Max(curMagnitude, activeShakes[i].curMagnitude);
                verticalPushMagnitude = Math.Max(verticalPushMagnitude,activeShakes[i].verticalMagnitude);

                activeShakes[i].curMagnitude -= activeShakes[i].magnitudeDecay * Time.unscaledDeltaTime;
                activeShakes[i].verticalMagnitude -= activeShakes[i].verticalMagnitudeDecay * Time.unscaledDeltaTime;

                if (activeShakes[i].curMagnitude <= 0f) {
                    activeShakes.RemoveAt(i);
                }
            }

            cameraShakeDummy.localPosition = Random.insideUnitSphere * curMagnitude * overallShakeAmount;
            if(rotationalShake)
                cameraShakeDummy.localRotation = Quaternion.Euler(verticalPushMagnitude*-1 * overallShakeAmount,horizontalPush*verticalPushMagnitude *overallShakeAmount,0);
            else 
                cameraShakeDummy.localRotation = Quaternion.identity;
            
            
            yield return 0;
        }
        cameraShakeDummy.localPosition = Vector3.zero;
        cameraShakeDummy.localRotation = quaternion.identity;
        //PlayerReference.s.playerHead.localPosition = originalLocalPosition;
        isShaking = false;
    }
}
