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

    public AudioClip warmUpClip;
    public AudioClip stopClip;
    
    
    public AudioSource introAudioSource;
    public AudioSource loopAudioSource;
    
    void PlayGunShoot () {
        introAudioSource.clip = warmUpClip;
        introAudioSource.Play();
        loopAudioSource.PlayDelayed(warmUpTime);
    }
    // Start is called before the first frame update
    void Start() {
        _gunModule = GetComponentInParent<GunModule>();
        
        if (_gunModule == null) {
            Debug.LogError("Can't find GunModule!");
            this.enabled = false;
            return;
        }
        
        _gunModule.startWarmUpEvent.AddListener(OnWarmUp);
        _gunModule.stopShootingEvent.AddListener(OnStopFiring);
        _gunModule.gatlingCountZeroEvent.AddListener(OnGatlingCountZero);
    }

    public bool isRotating = false;
    public float curSpeed = 0;
    void OnWarmUp() {
        warmUpTime = _gunModule.GetFireDelay();
        angleDelta = rotationSpeed / warmUpTime;
        isRotating = true;
        PlayGunShoot();
    }

    void OnStopFiring() {
        introAudioSource.Stop();
        loopAudioSource.Stop();
        introAudioSource.PlayOneShot(stopClip);
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

        if (curSpeed > 0.1f) {
            rotatingBit.Rotate(0, 0, curSpeed * Time.deltaTime);
        }
    }
}
