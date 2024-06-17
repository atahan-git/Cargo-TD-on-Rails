using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_MeteorShard : MonoBehaviour
{


	public void SetParticlesState(bool isOn) {
		var particles = GetComponentsInChildren<ParticleSystem>();

		foreach (var ps in particles) {
			if (isOn) {
				ps.Play();
			} else {
				ps.Stop();
			}
		}
	}
}
