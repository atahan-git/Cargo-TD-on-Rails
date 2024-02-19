using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakParticles : MonoBehaviour {
    private ParticleSystem[] _particleSystems;

    private Cart myCart;
    private void Start() {
        myCart = GetComponentInParent<Cart>();
        if (myCart == null)
            enabled = false;
        _particleSystems = GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < _particleSystems.Length; i++) {
            _particleSystems[i].Stop();
        }
    }

    private bool lastState ;
    void Update() {
        var isBreaking = SpeedController.s.currentBreakPower > 0 
                         && ((LevelReferences.s.speed > 0.01f || PlayStateMaster.s.isShop() || SpeedController.s.encounterOverride)
                             && myCart.GetComponentInParent<Train>());
        if (lastState != isBreaking) {
            for (int i = 0; i < _particleSystems.Length; i++) {
                if (isBreaking) {
                    _particleSystems[i].Play();
                } else {
                    _particleSystems[i].Stop();
                }
            }

            lastState = isBreaking;
        }

        if (isBreaking) {
            var particleAmount = LevelReferences.s.speed.Remap(0, 10, 10, 20);

            for (int i = 0; i < _particleSystems.Length; i++) {
                var emission = _particleSystems[i].emission;
                emission.rateOverTime = particleAmount;
            }
        }
    }
    
    
}
