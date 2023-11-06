using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class RandomClipPlayer : MonoBehaviour {
	public AudioClip[] clips;

	private AudioSource _audioSource;
	private Vector2 pitchRange = new Vector2(-0.1f,0.1f);

	private void Awake() {
		_audioSource = GetComponent<AudioSource>();
        
	}

	public void PlayClip(Vector3 position, float basePitch) {
		transform.position = position;
		_audioSource.clip = clips[Random.Range(0, clips.Length)];
		_audioSource.pitch = basePitch + Random.Range(pitchRange.x, pitchRange.y);
		_audioSource.Play();
	}
}