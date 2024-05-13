using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveWithGlobalMovement : MonoBehaviour {

	public float slowBuildupSpeedPercentPerSecond = 0.2f;

	public float currentSpeedPercent = 0f;

	private void Update() {
		transform.position -= LevelReferences.s.speed * Train.s.GetTrainForward() * currentSpeedPercent * Time.deltaTime;

		if (currentSpeedPercent < 1f) {
			currentSpeedPercent += slowBuildupSpeedPercentPerSecond *Time.deltaTime;
		} else {
			currentSpeedPercent = 1f;
		}
	}
}
