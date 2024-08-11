using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPitchAtStart : MonoBehaviour {

    public float pitchRange = 0.2f;

    void OnEnable() {
        GetComponent<AudioSource>().pitch = GetComponent<AudioSource>().pitch * (1f +  Random.Range(-pitchRange, pitchRange));
    }


    public void Play() {
        GetComponent<AudioSource>().pitch = GetComponent<AudioSource>().pitch * (1f + Random.Range(-pitchRange, pitchRange));
        if (GetComponent<FMODOneShotSource>()) {
            GetComponent<FMODOneShotSource>().Play();
        } else {
            GetComponent<AudioSource>().Play();
        }
    }

}
