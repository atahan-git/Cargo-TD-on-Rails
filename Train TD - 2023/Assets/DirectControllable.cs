using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectControllable : MonoBehaviour, IShowButtonOnCartUIDisplay, IResetState {

	public Transform cameraParent;

	public bool canUseAmmo = true;

	public enum DirectControlMode {
		Gun, LockOn
	}

	public DirectControlMode myMode;


	public Transform GetDirectControlTransform() {
		return cameraParent;
	}

	public Color GetColor() {
		return new Color(1f, 0, 0.9137254901960784f);
	}

	public void ResetState(int level) {
		var myGunModules = GetComponentsInChildren<GunModule>();
	}
}
