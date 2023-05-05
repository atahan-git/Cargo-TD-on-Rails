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


    void Activate() {
        GetComponentInChildren<AudioSource>()?.Play();
        foreach (var particle in GetComponentsInChildren<RandomParticleTurnOnAndOff>()) {
            particle.enabled = true;
        }
        
        foreach (var particle in GetComponentsInChildren<ParticleSystem>()) {
            particle.Play();
        }
    }

    void Disable() {
        GetComponentInChildren<AudioSource>()?.Stop();
        foreach (var particle in GetComponentsInChildren<RandomParticleTurnOnAndOff>()) {
            particle.enabled = false;
        }
        
        foreach (var particle in GetComponentsInChildren<ParticleSystem>()) {
            particle.Stop();
        }
    }
}
