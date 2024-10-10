using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomParticleTurnOnAndOff : MonoBehaviour {

    public Vector2 stayOnTime = new Vector2(0.5f, 1.5f);

    public Vector2 stayOfftime = new Vector2(2f, 4f);

    private ParticleSystem[] _particleSystems;
    private void Start() {
        _particleSystems = GetComponentsInChildren<ParticleSystem>();
    }

    void OnEnable()
    {
        Invoke("TurnOn",Random.Range(0.2f,4f));
    }

    private void OnDisable() {
        StopAllCoroutines();
        CancelInvoke();
        for (int i = 0; i < _particleSystems.Length; i++) {
            _particleSystems[i].Stop();
        }
    }

    public void TurnOff() {
        SetParticleStatus(false);
        
        Invoke("TurnOn",Random.Range(stayOfftime.x,stayOfftime.y));
    }

    public void TurnOn() {
        SetParticleStatus(true);
        
        Invoke("TurnOff",Random.Range(stayOnTime.x,stayOnTime.y));
    }

    void SetParticleStatus(bool isOn) {
        StartCoroutine(TieredStatusSet(isOn));
    }

    IEnumerator TieredStatusSet(bool isOn) {
        for (int i = 0; i < _particleSystems.Length; i++) {
            if (isOn) {
                _particleSystems[i].Play();
            } else {
                _particleSystems[i].Stop();
            }

            yield return new WaitForSeconds(0.5f);
        }

        yield return null;
    }
}
