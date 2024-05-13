using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlesDuringShop : MonoBehaviour {
    private void Start() {
        if (!PlayStateMaster.s.isCombatInProgress()) {
            Activate();
        } else {
            Disable();
        }
        
        PlayStateMaster.s.OnShopEntered.AddListener(Activate);
        PlayStateMaster.s.OnCombatEntered.AddListener(Disable);
    }

    private void OnDestroy() {
        PlayStateMaster.s.OnShopEntered.RemoveListener(Activate);
        PlayStateMaster.s.OnCombatEntered.RemoveListener(Disable);
    }


    private bool isActive = false;
    private void Update() {
        if (!Train.s.IsTrainMoving() && !MissionLoseFinisher.s.isMissionLost) {
            Activate();
        } else {
            Disable();
        }
    }


    void Activate() {
        if(isActive)
            return;
        isActive = true;
        
        Invoke(nameof(_Activate), 0.2f);
    }

    void _Activate() {
        if(!isActive)
            return;
        
        GetComponentInChildren<AudioSource>()?.Play();
        foreach (var particle in GetComponentsInChildren<RandomParticleTurnOnAndOff>()) {
            particle.enabled = true;
        }
        
        foreach (var particle in GetComponentsInChildren<ParticleSystem>()) {
            particle.Play();
        }
    }

    void Disable() {
        if(!isActive)
            return;
        isActive = false;
        GetComponentInChildren<AudioSource>()?.Stop();
        foreach (var particle in GetComponentsInChildren<RandomParticleTurnOnAndOff>()) {
            particle.enabled = false;
        }
        
        foreach (var particle in GetComponentsInChildren<ParticleSystem>()) {
            particle.Stop();
        }
    }
}
