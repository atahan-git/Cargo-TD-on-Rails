using FMODUnity;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserTurretAnimator : MonoBehaviour
{
    private GunModule _gunModule;

    private float warmUpTime;

    // Merxon: replaced by FMOD logic
    // public AudioClip warmUpClip;
    // public AudioClip stopClip;

    // public AudioSource introAudioSource;
    // public AudioSource loopAudioSource;

    #region FMOD Clip Handling
    [FoldoutGroup("FMOD SFX Handling")]
    public EventReference warmUpRef, stopRef;

    [FoldoutGroup("FMOD SFX Handling")]
    public FMODAudioSource loopAudioSource, onesShotSource;
    #endregion

    public GeroBeam myBeam;
    void PlayGunShoot () {
        // introAudioSource.clip = warmUpClip;
        // introAudioSource.Play();
        // loopAudioSource.PlayDelayed(warmUpTime);
        //onesShotSource.LoadClip(warmUpRef, true);
        //loopAudioSource.PlayDelayed(warmUpTime);
    }
    // Start is called before the first frame update
    void Start() {
        myBeam.gameObject.SetActive(true);
        _gunModule = GetComponentInParent<GunModule>();
        
        if (_gunModule == null) {
            Debug.LogError("Can't find GunModule!");
            this.enabled = false;
            return;
        }
        
        _gunModule.startShootingEvent.AddListener(OnStartShooting);
        _gunModule.stopShootingEvent.AddListener(OnStopShooting);
    }

    void OnStartShooting() {
        warmUpTime = _gunModule.GetFireDelay();
        PlayGunShoot();
        myBeam.ActivateBeam();
    }

    void OnStopShooting() {
        // introAudioSource.Stop();
        // loopAudioSource.Stop();
        // introAudioSource.PlayOneShot(stopClip);
        
        //loopAudioSource.Stop();
        //onesShotSource.LoadClip(stopRef, true);
        
        myBeam.DisableBeam();
    }
}
