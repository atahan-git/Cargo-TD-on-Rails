using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpGlow : MonoBehaviour,IActiveDuringCombat,IDisabledState {


	public bool isCartAlive = false;
	public bool isGlowActive = false;

	public float baseScale = 1;


	public void SetScale(float scale) {
		transform.localScale = scale * baseScale * Vector3.one;
	}

	public void SetGlowState(bool isGlowing) {
		isGlowActive = isGlowing;
		UpdateGlowState();
	}

	public void ActivateForCombat() {
		isCartAlive = true;
		UpdateGlowState();
	}

	public void Disable() {
		isCartAlive = false;
		UpdateGlowState();
	}
	
	public void CartDisabled() {
		isCartAlive = false;
		UpdateGlowState();
	}

	public void CartEnabled() {
		isCartAlive = true;
		UpdateGlowState();
	}


	void UpdateGlowState() {
		var shouldGlow = isCartAlive && isGlowActive;
		
		var particles = GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < particles.Length; i++) {
			if (shouldGlow) {
				particles[i].Play();
			} else {
				particles[i].Stop();
			}
		}
	}
}
