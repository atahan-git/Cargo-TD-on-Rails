using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CameraSwitcher : MonoBehaviour {


	public float switchTime = 3f;

	private CameraTarget[] _targets;

	public Transform curTarget;
	public bool isEngaged = false;


	private void Update() {
		if (isEngaged) {
			MainCameraReference.s.cam.transform.position = curTarget.position;
			MainCameraReference.s.cam.transform.rotation = curTarget.rotation;
			CameraController.s.AfterCameraPosUpdate?.Invoke();
		}
	}

	public void Engage() {
		_targets = GetComponentsInChildren<CameraTarget>();
		availableIndexes.Clear();
		PickTargetAndSwitch();
		isEngaged = true;
	}
	
	public void Disengage() {
		CancelInvoke();
		isEngaged = false;
	}


	private List<int> availableIndexes = new List<int>();
	private int current = -1;
	void PickTargetAndSwitch() {
		if (availableIndexes.Count == 0) {
			for (int i = 0; i < _targets.Length; i++) {
				availableIndexes.Add(i);
			}

			/*if (current != -1)
				availableIndexes.Remove(current);*/
		}

		current = availableIndexes[(current+1) % availableIndexes.Count];
		//availableIndexes.Remove(current);
		
		curTarget = _targets[current].transform;
		DepthOfFieldController.s.madeACameraJump = true;
		Invoke("PickTargetAndSwitch", switchTime);
	}
}
