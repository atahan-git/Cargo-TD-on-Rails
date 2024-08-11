using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveWithGlobalMovement : MonoBehaviour {

	public float slowBuildupSpeedPercentPerSecond = 0.2f;

	public float currentSpeedPercent = 0f;
	public float startSpeedPercent = 0;

	private void Update() {
		//var actualPercent = 1f - currentSpeedPercent;
		var actualPercent = currentSpeedPercent;
		transform.position -= LevelReferences.s.speed * Train.s.GetTrainForward() * actualPercent * Time.deltaTime;

		if (currentSpeedPercent < 1f) {
			currentSpeedPercent += slowBuildupSpeedPercentPerSecond *Time.deltaTime;
		} else {
			currentSpeedPercent = 1f;
		}
	}
}
