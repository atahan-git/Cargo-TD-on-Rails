using FMODUnity;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserTurretAnimator : MonoBehaviour
{
    private GunModule _gunModule;

    private float warmUpTime;

    #region FMOD Clip Handling
    [FoldoutGroup("FMOD SFX Handling")]
    public EventReference warmUpRef, stopRef;

    [FoldoutGroup("FMOD SFX Handling")]
    public FMODAudioSource loopAudioSource;

    [FoldoutGroup("FMOD SFX Handling")]
    [ReadOnly, ShowInInspector]
    private float laserPower, targetLaserPower;
    #endregion

    public GeroBeam myBeam;
    void PlayGunShoot () {
        AudioManager.PlayOneShot(warmUpRef);
        targetLaserPower = 1;
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
        
        _gunModule.startWarmUpEvent.AddListener(OnWarmUp);
        _gunModule.stopShootingEvent.AddListener(OnStopShooting);
    }

    private void Update()
    {
        laserPower = Mathf.MoveTowards(laserPower, targetLaserPower, Time.deltaTime * (1 / warmUpTime));
        loopAudioSource.SetParamByName("LaserPower", laserPower);
    }

    void OnWarmUp() {
        warmUpTime = _gunModule.GetFireDelay();
        PlayGunShoot();
        myBeam.ActivateBeam();
    }

    void OnStopShooting() {
        AudioManager.PlayOneShot(stopRef);
        targetLaserPower = 0;

        myBeam.DisableBeam();
    }
}
