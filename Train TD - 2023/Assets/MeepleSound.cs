using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class MeepleSound : MonoBehaviour {

    public bool playAutomatically;
    [ShowIf("playAutomatically")]
    public Vector2 playDelay = new Vector2(2, 200);

    public AudioClip[] clips;

    private void Start() {
        if (playAutomatically) {
            Invoke(nameof(PlayRepeating), Random.Range(playDelay.x, playDelay.y));
        }
    }

    void PlayRepeating() {
        PlayClip(); 
        Invoke(nameof(PlayRepeating), Random.Range(playDelay.x, playDelay.y));  
    }

    [Button]
    public void PlayClip() {
        GetComponent<AudioSource>().PlayOneShot(clips[Random.Range(0,clips.Length)]);
    }
}
