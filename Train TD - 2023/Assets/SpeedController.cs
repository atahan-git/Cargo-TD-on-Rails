using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SpeedController : MonoBehaviour, IShowOnDistanceRadar {
    public static SpeedController s;

    private void Awake() {
        s = this;
    }

    public float debugSpeedOverride = -1f;

    public float currentTime = 0;
    public float currentDistance = 0;

    public float missionDistance = 300; //100 engine power goes 1 distance per second
    public bool missionEndSet = false;
    
    public TMP_Text timeText;
    public TMP_Text distanceText;

    public TrainStation endTrainStation;
    
    public List<EngineModule> engines = new List<EngineModule>();

    private void Start() {
        ResetDistance();
    }

    public void ResetDistance() {
        missionDistance = 500;
        missionEndSet = false;
        currentDistance = 0;
        LevelReferences.s.speed = 0;
        internalRealSpeed = 0;
        targetSpeed = 0;
        
        currentBreakPower = 0;
    }

    public void SetUpOnMissionStart() {
        ResetDistance();
        DisableLowPower();
        PlayEngineStartEffects();
    }

    public void RegisterRadar() {
        DistanceAndEnemyRadarController.s.RegisterUnit(this);
    }

    public void IncreaseMissionEndDistance(float amount) {
        SetMissionEndDistance(missionDistance + amount);
    }

    public void TravelToMissionEndDistance(bool isShowingPrevRewards) {
        CalculateStopAcceleration();
        currentDistance = missionDistance;
        missionEndSet = true;
    }

    public void SetMissionEndDistance(float distance) {
        missionDistance = distance;
        endTrainStation.stationDistance = missionDistance;
        missionEndSet = true;
        //Debug.LogError("Re add hex grid here");
        //HexGrid.s.ResetDistance();
    }


    public int cartCapacity;
    public int cartCapacityModifier = 0;
    public float targetSpeed = 0;
    public float speedMultiplier = 1;
    public float speedAmount = 0;
    public float acceleration = 0;
    public bool delicateMachinery = false;
    public void ResetMultipliers() {
        cartCapacityModifier = 0;
        speedMultiplier = 1;
        speedAmount = 0;
        delicateMachinery = false;
        boostEffectMultiplier = 1;
    }
    
    public void AddEngine(EngineModule engineModule) {
        engines.Add(engineModule);
        CalculateSpeedBasedOnCartCapacity();
    }

    public void RemoveEngine(EngineModule engineModule) {
        engines.Remove(engineModule);
        CalculateSpeedBasedOnCartCapacity();
    }


    public UnityEvent OnSpeedChangedBasedOnCartCapacity = new UnityEvent();
    public void CalculateSpeedBasedOnCartCapacity() {
        cartCapacity = 0;
        targetSpeed = 0;
        for (int i = 0; i < engines.Count; i++) {
            cartCapacity += (int)((engines[i].enginePower + engines[i].extraEnginePower) * (engines[i].isHalfPower ? 0.5f : 1f));
            targetSpeed += (int)((engines[i].speedAdd + engines[i].extraSpeedAdd) * (engines[i].isHalfPower ? 0.5f : 1f));
        }

        targetSpeed += speedAmount;
        targetSpeed *= speedMultiplier;
        cartCapacity += cartCapacityModifier;

        var cartCount = Train.s.carts.Count - 1;// main engine itself doesn't count hence +1

        var excessCarts = cartCount - cartCapacity;

        if (!delicateMachinery) {
            if (excessCarts > 0) {
                switch (excessCarts) {
                    case 1:
                        targetSpeed *= 0.95f;
                        break;
                    case 2:
                        targetSpeed *= 0.85f;
                        break;
                    default: // more than 2
                        targetSpeed *= 0.75f;
                        break;
                }
            }
        } else {
            if (excessCarts != 0)
                targetSpeed *= 0.25f;
        }

        if (excessCarts <= 0) {
            acceleration = ((float)Mathf.Clamp(cartCount, 3, cartCapacity)).Remap(3, cartCapacity, 1, 0.3f);
        } else {
            acceleration = ((float)Mathf.Clamp(excessCarts, 0, 5)).Remap(0, 5, 0.2f, 0.01f);
        }

        OnSpeedChangedBasedOnCartCapacity?.Invoke();
    }

    public MiniGUI_SpeedDisplayArea speedDisplayArea;
    public MiniGUI_SpeedDisplayArea speedDisplayAreaShop;

    public float breakPower = 1f;

    public float internalRealSpeed;
    public int activeEngines = 0;

    public float enginePowerPlayerControl = 1f;
    public float currentBreakPower = 0;

    public bool encounterOverride = false;

    public void PlayEngineStartEffects() {
        for (int i = 0; i < engines.Count; i++) {
            engines[i].OnEngineStart?.Invoke();
        }
    }
    
    public void SetEngineBoostEffects(bool isBoosting, bool isLowPower) {
        for (int i = 0; i < engines.Count; i++) {
            engines[i].OnEngineBoost?.Invoke(isBoosting);
        }
        for (int i = 0; i < engines.Count; i++) {
            engines[i].OnEngineLowPower?.Invoke(isLowPower);
        }
    }

    public float maxSpeed = 8;
    
    private void Update() {
        CalculateSpeedBasedOnCartCapacity();

        if (isBoosting || isSlow) {
            targetSpeed *= currentBoostMultiplier;
            if (isBoosting) {
                boostTimer -= Time.deltaTime;
            } else {
                boostTimer += Time.deltaTime;
            }
        }

        speedDisplayArea.UpdateValues(targetSpeed,  LevelReferences.s.speed);
        speedDisplayAreaShop.UpdateValues(targetSpeed, LevelReferences.s.speed);


        if (!encounterOverride) {
            if (PlayStateMaster.s.isCombatInProgress()) {


                var realAcc = acceleration;
                if (targetSpeed < internalRealSpeed) {
                    realAcc += 0.2f; // we slow down faster than we speed up so that speed boost isn't cheaty
                }
                internalRealSpeed = Mathf.MoveTowards(internalRealSpeed, targetSpeed, realAcc * Time.deltaTime);
                slowAmount = Mathf.Clamp(slowAmount, 0, 1.5f*(internalRealSpeed - 0.1f));
                LevelReferences.s.speed = Mathf.Max(internalRealSpeed - (slowAmount/1.5f), 0f);

                if (debugSpeedOverride > 0) {
                    LevelReferences.s.speed = debugSpeedOverride;
                }

                slowAmount = Mathf.Lerp(slowAmount, 0, slowDecay * Time.deltaTime);
                slowAmount = Mathf.Clamp(slowAmount, 0, 5);
                if (slowAmount <= 0.2f) {
                    ToggleSlowedEffect(false);
                }

                currentTime += Time.deltaTime;
                timeText.text = GetNiceTime(currentTime);

                currentDistance += LevelReferences.s.speed * Time.deltaTime;

                distanceText.text = ((int)currentDistance).ToString();

                if (missionEndSet && currentDistance > missionDistance) {
                    MissionWinFinisher.s.MissionWon();
                    CalculateStopAcceleration();
                }
            } else if (PlayStateMaster.s.isCombatFinished() && !MissionLoseFinisher.s.isMissionLost) {
                var stopProgress = (currentDistance - beforeStopDistance) / stopLength;
                if (currentDistance >= stopMissionDistanceTarget) {
                    stopProgress = 1;
                }

                if (stopProgress < 1) {
                    LevelReferences.s.speed = Mathf.Lerp(beforeStopSpeed, 0, stopProgress * stopProgress);
                    LevelReferences.s.speed = Mathf.Clamp(LevelReferences.s.speed, 0.2f, float.MaxValue);

                    currentBreakPower = 10;

                    currentDistance += LevelReferences.s.speed * Time.deltaTime;
                } else {
                    LevelReferences.s.speed = 0;
                    currentBreakPower = 0;
                    currentDistance = stopMissionDistanceTarget;
                }
            } else {
                LevelReferences.s.speed = 0;
            }
        }
    }

    //public readonly float stopDistance = 10f;
    public readonly float stopDistance = 7.5f;
    public float stopMissionDistanceTarget;
    public float beforeStopSpeed;
    public float beforeStopDistance;
    public float stopLength;
    void CalculateStopAcceleration() {
        beforeStopSpeed = LevelReferences.s.speed;
        if (beforeStopSpeed < 2f) {
            LevelReferences.s.speed = 4;
            beforeStopSpeed = LevelReferences.s.speed;
        }

        stopLength = stopDistance/* - (Train.s.GetTrainLength()/2f)*/;
        stopMissionDistanceTarget = missionDistance + stopLength;
        beforeStopDistance = missionDistance;
    }

    public static string GetNiceTime(float time) {
        var minutes = (int) (time / 60);
        var remainingSeconds = (int) (time - minutes * 60);
        return (minutes.ToString("00") + ':' + remainingSeconds.ToString("00"));
    }

    public string GetCurrentTime() {
        return GetNiceTime(currentTime);
    }


    public void UseSteam(float amount) {
        // not used anymore
    }

    [Header("Boost stuff")]
    public bool canBoost = true;
    public bool isBoosting = false;
    public bool isSlow = false;
    public float boostDuration = 30f;
    public float boostMultiplier = 2f;
    public float lowPowerMultiplier = 0.5f;
    public float boostEffectMultiplier = 1f;
    public float lowPowerDuration = 60f;
    public float currentBoostMultiplier = 1;
    public float boostTimer;
    public float boostTotalTime;
    public float boostGraphicPercent {
        get {
            return Mathf.Clamp((boostTimer*2) / boostTotalTime,0,2f);
        }
    }
[HideInInspector]
    public UnityEvent OnSpeedBoostActivated = new UnityEvent();
    public void ActivateBoost() {
        if (canBoost && !encounterOverride) {
            for (int i = 0; i < engines.Count; i++) {
                var boostable = engines[i].GetComponentInChildren<EngineBoostable>(true);
                if (boostable) {
                    boostable.gameObject.SetActive(false);
                }
            }

            isBoosting = true;

            if (!PlayerWorldInteractionController.s.engineBoostDamageInstead) {
                currentBoostMultiplier = Mathf.Clamp((boostMultiplier-1) * boostEffectMultiplier,0,2) + 1;
            } else {
                var boostable = Train.s.GetComponentInChildren<EngineBoostable>(true);
                if (boostable != null) {
                    boostable.engineDamageBoostActive = true;
                    boostable.engineDamageReductionActive = false;
                }
                Train.s.ArtifactsChanged();
            }

            PlayEngineStartEffects();
            SetEngineBoostEffects(true, false);
            CameraController.s.BoostFOV();
            boostTimer = boostDuration;
            boostTotalTime = boostDuration;
            Invoke(nameof(DisableBoostAndActivateLowPowerMode), boostDuration);
            OnSpeedBoostActivated?.Invoke();
        }
    }


    void DisableBoostAndActivateLowPowerMode() {
        if (!PlayerWorldInteractionController.s.engineBoostDamageInstead) {
            currentBoostMultiplier = 1- Mathf.Clamp(lowPowerMultiplier / Mathf.Max(0.01f,boostEffectMultiplier), 0, 0.95f);;
        } else {
            var boostable = Train.s.GetComponentInChildren<EngineBoostable>(true);
            if (boostable != null) {
                boostable.engineDamageBoostActive = false;
                boostable.engineDamageReductionActive = true;
            }

            Train.s.ArtifactsChanged();
        }
        CameraController.s.SlowFOV();

        isSlow = true;
        isBoosting = false;
    
        boostTimer = 0;
        boostTotalTime = lowPowerDuration;
        SetEngineBoostEffects(false, true);
        Invoke(nameof(DisableLowPower), lowPowerDuration);
    }

    public void DisableLowPower() {
        CancelInvoke(nameof(DisableBoostAndActivateLowPowerMode));
        CancelInvoke(nameof(DisableLowPower));
        
        SetEngineBoostEffects(false, false);
        PlayEngineStartEffects();
        CameraController.s.ReturnToRegularFOV();
        
        canBoost = true;
        
        if (!PlayerWorldInteractionController.s.engineBoostDamageInstead) {
            currentBoostMultiplier = 1;
        } else {
            var boostable = Train.s.GetComponentInChildren<EngineBoostable>(true);
            if (boostable != null) {
                boostable.engineDamageBoostActive = false;
                boostable.engineDamageReductionActive = false;
            }

            Train.s.ArtifactsChanged();
        }
        
        isBoosting = false;
        isSlow = false;
        
        
        boostTimer = 1;
        boostTotalTime = 2;
        
        for (int i = 0; i < engines.Count; i++) {
            var boostable = engines[i].GetComponentInChildren<EngineBoostable>(true);
            if (boostable) {
                boostable.gameObject.SetActive(true);
            }
        }
    }

    public void OnCombatFinished(bool realCombat) {
        DisableLowPower();
    }

    public float GetDistance() {
        return currentDistance;
    }

    public Sprite trainRadarImg;

    public bool IsTrain() {
        return true;
    }
    
    public Sprite GetIcon() {
        return trainRadarImg;
    }

    public bool isLeftUnit() {
        return false;
    }

    public float slowMultiplier = 0.5f;
    public float slowAmount;
    public float slowDecay = 0.1f;
    public void AddSlow(float amount) {
        amount *= slowMultiplier;
        if (slowAmount > 1)
            amount /= slowAmount;
        slowAmount += amount;
        ToggleSlowedEffect(true);
    }


    public List<GameObject> activeSlowedEffects = new List<GameObject>();
    private bool isSlowedOn = false;
    void ToggleSlowedEffect(bool isOn) {
        if (isOn && !isSlowedOn) {
            for (int i = 0; i < engines.Count; i++) {
                var effect = Instantiate(LevelReferences.s.currentlySlowedEffect, engines[i].transform.position, Quaternion.identity);
                effect.transform.SetParent(engines[i].transform);
                activeSlowedEffects.Add(effect);
            }

            isSlowedOn = true;
        }

        if (!isOn && isSlowedOn) {
            for (int i = 0; i < activeSlowedEffects.Count; i++) {
                SmartDestroy(activeSlowedEffects[i].gameObject);
            }
            
            activeSlowedEffects.Clear();
            isSlowedOn = false;
        }
    }
    
    void SmartDestroy(GameObject target) {
        var particles = GetComponentsInChildren<ParticleSystem>();

        foreach (var particle in particles) {
            particle.transform.SetParent(null);
            particle.Stop();
            Destroy(particle.gameObject, 1f);
        }
            
        Destroy(target);
    }
}
