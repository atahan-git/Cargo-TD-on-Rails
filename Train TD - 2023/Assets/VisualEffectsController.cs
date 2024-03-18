using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualEffectsController : MonoBehaviour {

	public static VisualEffectsController s;

	private void Awake() {
		s = this;
	}

	public enum EffectPriority {
		Always,
		High,
		Medium,
		Low
	}

	public GameObject SmartInstantiate(GameObject myObj, Vector3 position, Quaternion rotation, EffectPriority priority = EffectPriority.Always) {
		var obj = Instantiate(myObj, transform);
		obj.transform.position = position;
		obj.transform.rotation = rotation;
		return obj;
	}
	
	public GameObject SmartInstantiate(GameObject myObj, Transform parent, EffectPriority priority = EffectPriority.Always) {
		var obj = Instantiate(myObj, parent);
		return obj;
	}
}
