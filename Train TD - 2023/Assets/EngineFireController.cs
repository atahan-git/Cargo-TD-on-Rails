using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineFireController : MonoBehaviour {
    private ParticleSystem _particleSystem;
    private AudioSource _audio;

    public float baseDelay = 0.4f;
    public float minDelay = 0.1f;
    public float speedDelayEffect = 0.1f;
    public float currentDelay = 0.4f;

    public float slowSoundMaxSpeed = 1f;
    public float mediumSoundMaxSpeed = 7f;

    public AudioClip[] speedSounds;

    //public GameObject soundPrefab;

    public static EngineFireController soundSource;

    private EngineModule _engine;

    private void OnEnable() {
	    if (soundSource == null)
		    soundSource = this;
    }

    private void OnDisable() {
	    if (soundSource == this)
		    soundSource = null;
    }

    void Start() {
	    _particleSystem = GetComponentInChildren<ParticleSystem>();
	    _audio = GetComponentInChildren<AudioSource>();
	    _engine = GetComponentInParent<EngineModule>();
	    UpdateEngineParticleSystemValues();
    }

    private float lastSpeed = 0;
    private float lastEnginePowerPlayerControl = 1;
    public int lastSpeedTier = 0;
    //public float pitchRange = 0.2f;
    void Update()
    {
	    if (Mathf.Abs(LevelReferences.s.speed - lastSpeed) > 0.1f) {
		    lastSpeed = LevelReferences.s.speed;
		    UpdateEngineParticleSystemValues();
	    }

	    if (Mathf.Abs(lastEnginePowerPlayerControl - SpeedController.s.enginePowerPlayerControl) > 0.1f) {
		    lastEnginePowerPlayerControl = SpeedController.s.enginePowerPlayerControl;
		    UpdateEngineParticleSystemValues();
	    }
	    
	    UpdateEngineParticleSystemValues();
	    

	    if (PlayStateMaster.s.isCombatInProgress()) {
		    var speedTier = -1;

		    if (LevelReferences.s.speed > 0)
			    speedTier += 1;
		    if (LevelReferences.s.speed > slowSoundMaxSpeed)
			    speedTier += 1;
		    if (LevelReferences.s.speed > mediumSoundMaxSpeed)
			    speedTier += 1;

		    if (speedTier != lastSpeedTier) {
			    StopAllCoroutines();
			    if (speedTier >= 0) {
				    StartCoroutine(ChangeSpeedSound(speedSounds[speedTier]));
			    } else {
				    _audio.loop = false;
			    }

			    lastSpeedTier = speedTier;
		    }
	    }

		// for FMOD
		UpdateLocomotiveSound();
    }

    IEnumerator ChangeSpeedSound(AudioClip target) {
	    if (soundSource == null) {
            soundSource = this;
        }
	    
	    if (soundSource == this) {
		    _audio.loop = false;
		    while (_audio.isPlaying) {
			    yield return null;
		    }

		    _audio.clip = target;
		    _audio.loop = true;
		    _audio.Play();
	    } else {
		    _audio.Stop();
	    }
    }


    public bool isLevelFinishTriggered = false;
    public bool fireActive = true;
    void UpdateEngineParticleSystemValues() {
	    var emissionModule = _particleSystem.emission;
	    var mainModule = _particleSystem.main;
	    if (!PlayStateMaster.s.isCombatStarted() || !fireActive) {
		    emissionModule.enabled = false;
	    } else if(lastSpeed > 0.2f && lastEnginePowerPlayerControl > 0) {
		    emissionModule.enabled = true;
		    var burst = emissionModule.GetBurst(0);
		    currentDelay = Mathf.Max(baseDelay/(((LevelReferences.s.speed-1)*speedDelayEffect)+1), minDelay);
		    burst.repeatInterval = currentDelay;
		    emissionModule.SetBurst(0, burst);
		    var forceOverLifeTime = _particleSystem.forceOverLifetime;
		    forceOverLifeTime.y = new ParticleSystem.MinMaxCurve(LevelReferences.s.speed * 25f);

		    // Engine Boost
		    /*var playerControlAdjusted = lastEnginePowerPlayerControl;
		    /*var powerLow = lastEnginePowerPlayerControl.Remap(0, 1.5f,6,60) * (_engine.enginePower / 6f);
		    var powerHigh = lastEnginePowerPlayerControl.Remap(0, 1.5f,8,65) * (_engine.enginePower / 6f);#1#
		    var powerLow = 20 /*+ (_engine.enginePower / 6f)#1#;
		    var powerHigh = 25 /*+ (_engine.enginePower / 6f)#1#;
		    mainModule.startSpeed = new ParticleSystem.MinMaxCurve(powerLow, powerHigh);*/
		    /*if (lastEnginePower > 300) {
			    mainModule.startSpeed = new ParticleSystem.MinMaxCurve(50, 80);
		    }else if(lastEnginePower > 150){
			    mainModule.startSpeed = new ParticleSystem.MinMaxCurve(20, 40);
			} else {
			    mainModule.startSpeed = new ParticleSystem.MinMaxCurve(5, 14);
		    }*/
	    } else {
		    emissionModule.enabled = false;
		    if (PlayStateMaster.s.isCombatFinished() && !isLevelFinishTriggered) {
			    isLevelFinishTriggered = true;
			    _audio.clip = speedSounds[0];
			    _particleSystem.Stop();
			    _audio.loop = false;
		    }
	    }
    }

    public void StopEngineFire() {
	    fireActive = false;
    }

    public void ActivateEngineFire() {
	    fireActive = true;
    }

	[Header("FMOD Engine Sound")]
	// FMOD locmotive sound
	public FMODAudioSource speaker;

	void UpdateLocomotiveSound()
	{
        speaker.SetParamByName("LocomotiveSpeed", LevelReferences.s.speed * 0.2f);

		speaker.volume = Mathf.Lerp(speaker.volume, FMODMusicPlayer.s.numOfEngagingWave == 0 ? 0.8f : 0.5f, Time.unscaledDeltaTime * 3);
	}
}
