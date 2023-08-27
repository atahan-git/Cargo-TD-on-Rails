using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GatlingAnimator : MonoBehaviour {
    private GunModule _gunModule;

    public Transform rotatingBit;
    public float rotationSpeed = 1800;
    private float angleDelta;
    private float warmUpTime;
    public float slowDownDelta = 900;

    [FoldoutGroup("Audio")]
    public FMODAudioSource revSpeaker;
    
    private bool easterEggSidewaysRotate = false;
    void Start() {
        _gunModule = GetComponentInParent<GunModule>();
        
        if (_gunModule == null) {
            Debug.LogError("Can't find GunModule!");
            this.enabled = false;
            return;
        }
        
        _gunModule.startWarmUpEvent.AddListener(OnWarmUp);
        _gunModule.gatlingCountZeroEvent.AddListener(OnGatlingCountZero);


        easterEggSidewaysRotate = EasterEggController.s.GetEasterEggDisplay(EasterEggController.EasterEggChances.rare4);
    }

    public bool isRotating = false;
    public float curSpeed = 0;
    void OnWarmUp() {
        warmUpTime = _gunModule.GetFireDelay();
        angleDelta = rotationSpeed / warmUpTime;
        isRotating = true;
    }
    
    void OnGatlingCountZero() {
        isRotating = false;
        /*introAudioSource.Stop();
        loopAudioSource.Stop();
        introAudioSource.PlayOneShot(stopClip);*/
    }
    
    private void Update() {
        if (isRotating) {
            curSpeed = Mathf.MoveTowards(curSpeed, rotationSpeed * (1f / (_gunModule.GetFireDelay() * 10)), angleDelta * Time.deltaTime);
        } else {
            curSpeed = Mathf.MoveTowards(curSpeed, 0, slowDownDelta * Time.deltaTime);
        }

        //Debug.Log(curSpeed);
        revSpeaker.SetParamByName("GatlingRevSpeed", curSpeed / rotationSpeed);

        if (curSpeed > 0.1f) {
            if (easterEggSidewaysRotate) { 
                rotatingBit.Rotate(0, curSpeed * Time.deltaTime, 0);
                
            } else {
                rotatingBit.Rotate(0, 0, curSpeed * Time.deltaTime);
            }
        }
    }
}
