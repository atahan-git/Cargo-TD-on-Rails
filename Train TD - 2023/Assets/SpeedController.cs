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
        missionDistance = float.MaxValue;
        missionEndSet = false;
        currentDistance = 0;
        LevelReferences.s.speed = 0;
        internalRealSpeed = 0;
        targetSpeed = 0;
        
        currentBreakPower = 0;
    }

    public void SetUpOnMissionStart() {
        ResetDistance();
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
        //endTrainStation.stationDistance = missionDistance;
        missionEndSet = true;
        //Debug.LogError("Re add hex grid here");
        //HexGrid.s.ResetDistance();
    }

    public void SetWarpMode(bool _isWarping) {
        isWarping = _isWarping;
        CalculateSpeedBasedOnCartCapacity();
    }

    public void SetWarpTargetSpeed(float _targetSpeed) {
        targetSpeed = _targetSpeed;
    }

    public bool IsMoving() {
        return internalRealSpeed > 0;
    }

    public void SetBrakingStatus(bool isBraking) {
        if (isBraking) {
            currentBreakPower = 1;
            targetSpeed = 0;
        } else {
            currentBreakPower = 0;
            CalculateSpeedBasedOnCartCapacity();
        }
    }

    public bool IsBraking() {
        return currentBreakPower > 0;
    }


    public bool isWarping = false;


    public float cartCapacity;
    public float targetSpeed = 0;
    public float currentPressure = 0;
    public float acceleration = 0;

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
        if(isWarping)
            return;
        
        cartCapacity = 0;
        currentPressure = 0;
        var newTargetSpeed = 0f;
        for (int i = 0; i < engines.Count; i++) {
            cartCapacity += engines[i].GetEnginePower();
            newTargetSpeed += engines[i].GetSpeedAdd();
            currentPressure += engines[i].GetEffectivePressure();
        }

        var cartCount = Train.s.carts.Count - 1;// main engine itself doesn't count hence +1

        var excessCarts = cartCount - cartCapacity;

        if (excessCarts > 0) {
            switch (excessCarts) {
                case 1:
                    newTargetSpeed *= 0.95f;
                    break;
                case 2:
                    newTargetSpeed *= 0.85f;
                    break;
                default: // more than 2
                    newTargetSpeed *= 0.75f;
                    break;
            }
        }

        if (excessCarts <= 0) {
            acceleration = ((float)Mathf.Clamp(cartCount, 3, cartCapacity)).Remap(3, cartCapacity, 1, 0.3f);
        } else {
            acceleration = ((float)Mathf.Clamp(excessCarts, 0, 5)).Remap(0, 5, 0.2f, 0.01f);
        }
        
        //newTargetSpeed *= TweakablesMaster.s.GetTrainSpeedMultiplier();

        if (currentBreakPower <= 0) {
            if (!Mathf.Approximately(targetSpeed, newTargetSpeed)) {
                OnSpeedChangedBasedOnCartCapacity?.Invoke();
            }
            targetSpeed = newTargetSpeed;
        }
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
    
    private void Update() {
        CalculateSpeedBasedOnCartCapacity();

        speedDisplayArea.UpdateValues(targetSpeed,  LevelReferences.s.speed);
        speedDisplayAreaShop.UpdateValues(targetSpeed, LevelReferences.s.speed);
        
        if (!encounterOverride) {
            if (PlayStateMaster.s.isCombatInProgress()) {
                var realAcc = acceleration;
                if (targetSpeed < internalRealSpeed) {
                    realAcc += 0.2f; // we slow down faster than we speed up so that speed boost isn't cheaty
                }

                realAcc /= 3f;
                
                if (currentBreakPower > 0) {
                    realAcc = Mathf.Max(realAcc, currentBreakPower);
                }

                internalRealSpeed = Mathf.MoveTowards(internalRealSpeed, targetSpeed, realAcc * Time.deltaTime);
                slowAmount = Mathf.Clamp(slowAmount, 0, 1.5f*(internalRealSpeed - 0.1f));
                if (slowAmount < 0) {
                    slowAmount = 0;
                }
                
                LevelReferences.s.speed = Mathf.Max(internalRealSpeed - (slowAmount/1.5f), 0f);

                if (debugSpeedOverride > 0) {
                    LevelReferences.s.speed = debugSpeedOverride;
                }

                slowAmount = Mathf.MoveTowards(slowAmount, 0, slowDecay * Time.deltaTime);
                slowAmount = Mathf.Lerp(slowAmount, 0, slowDecay * Time.deltaTime);
                slowAmount = Mathf.Clamp(slowAmount, 0, 5);
                if (slowAmount <= 0.2f) {
                    ToggleSlowedEffect(false);
                }

                if (CameraController.s.isSnappedToMap) {
                    LevelReferences.s.speed = 0;
                }

                if(!CheatsController.s.stopMoving)
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

                    currentBreakPower = 1;

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
                var effect = VisualEffectsController.s.SmartInstantiate(LevelReferences.s.currentlySlowedEffect, engines[i].transform.position, Quaternion.identity);
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
