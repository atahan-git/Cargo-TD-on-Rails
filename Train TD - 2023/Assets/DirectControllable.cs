using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectControllable : MonoBehaviour, IShowButtonOnCartUIDisplay, IResetState {

	public Transform cameraParent;
	
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

	private bool isFire = false;
	private bool isExplosive = false;
	private bool isSticky = false;
	public void ResetState(int level) {
		isFire = false;
		isExplosive = false;
		isSticky = false;
		var myGunModules = GetComponentsInChildren<GunModule>();
		for (int i = 0; i < myGunModules.Length; i++) {
			myGunModules[i].isFire = isFire;
			myGunModules[i].isSticky = isSticky;
			myGunModules[i].isExplosive = isExplosive;
		}
	}
	
	public void ApplyBulletEffect(ModuleAmmo.AmmoEffects effect) {
		switch (effect) {
			case ModuleAmmo.AmmoEffects.fire:
				if (!isFire) {
					isFire = true;
				}

				break;
			case ModuleAmmo.AmmoEffects.sticky:
				if (!isSticky) {
					isSticky = true;
				}
				break;
			case ModuleAmmo.AmmoEffects.explosive:
				if (!isExplosive) {
					isExplosive = true;
				}
				break;
		}
        
		var myGunModules = GetComponentsInChildren<GunModule>();
		for (int i = 0; i < myGunModules.Length; i++) {
			myGunModules[i].isFire = isFire;
			myGunModules[i].isSticky = isSticky;
			myGunModules[i].isExplosive = isExplosive;
		}
	}
}
