using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GatlingAnimator : MonoBehaviour {
    private GunModule _gunModule;

    public Transform rotatingBit;
    public float rotationSpeedMultiplier = 1;

    [FoldoutGroup("Audio")]
    public FMODAudioSource revSpeaker;
    
    private bool easterEggSidewaysRotate = false;

    public bool playSound = true;
    void Start() {
        _gunModule = GetComponentInParent<GunModule>();
        
        if (_gunModule == null) {
            Debug.LogError("Can't find GunModule!");
            this.enabled = false;
            return;
        }
        
        _gunModule.startShootingEvent.AddListener(OnStartShooting);
        _gunModule.stopShootingEvent.AddListener(OnStopShooting);


        easterEggSidewaysRotate = EasterEggController.s.GetEasterEggDisplay(EasterEggController.EasterEggChances.rare4);
    }

    public bool isRotating = false;
    public float curSpeed = 0;
    void OnStartShooting() {
        isRotating = true;
    }
    
    void OnStopShooting() {
        isRotating = false;
        /*introAudioSource.Stop();
        loopAudioSource.Stop();
        introAudioSource.PlayOneShot(stopClip);*/
    }
    
    private void Update() {
        var curRotationSpeed = (1 / _gunModule.GetFireDelay()) * 60 * rotationSpeedMultiplier;
        var maxRotationSpeed = (1 / _gunModule.GetFireDelayAtGatlingPercent(1)) * 60 * rotationSpeedMultiplier;
        
        //print(curRotationSpeed);
        if (isRotating) {
            curSpeed = Mathf.MoveTowards(curSpeed, curRotationSpeed, 360 * Time.deltaTime);
        } else {
            curSpeed = Mathf.MoveTowards(curSpeed, _gunModule.GetCurrentGatlingPercent()*curRotationSpeed, 360 * Time.deltaTime);
        }

        //Debug.Log(curSpeed);
        if (playSound) {
            revSpeaker.SetParamByName("GatlingRevSpeed", curSpeed / maxRotationSpeed);
        }

        if (curSpeed > 0.1f) {
            if (easterEggSidewaysRotate) { 
                rotatingBit.Rotate(0, curSpeed * Time.deltaTime, 0);
                
            } else {
                rotatingBit.Rotate(0, 0, curSpeed * Time.deltaTime);
            }
        }
    }
}
