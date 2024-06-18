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

	
	private void Update() {
		mediumEffectCount -= 10*Time.deltaTime;
		mediumEffectCount = Mathf.Clamp(mediumEffectCount, 0, 5);
	}

	private float mediumEffectCount = 0;
	public GameObject SmartInstantiate(GameObject myObj, Vector3 position, Quaternion rotation, EffectPriority priority = EffectPriority.Always) {
		if (priority == EffectPriority.Medium) {
			mediumEffectCount += 1;
			if (mediumEffectCount > 5) {
				return null;
			}
		}
		
		var obj = Instantiate(myObj, transform);
		obj.transform.position = position;
		obj.transform.rotation = rotation;
		return obj;
	}
	
	public GameObject SmartInstantiate(GameObject myObj, Transform parent, EffectPriority priority = EffectPriority.Always) {
		var obj = Instantiate(myObj, parent);
		obj.transform.localPosition = Vector3.zero;
		obj.transform.localRotation = Quaternion.identity;
		return obj;
	}
	
	public GameObject SmartInstantiate(GameObject myObj, Transform parent, Vector3 position, Quaternion rotation, EffectPriority priority = EffectPriority.Always) {
		var obj = Instantiate(myObj, parent);
		obj.transform.position = position;
		obj.transform.rotation =rotation;
		return obj;
	}
}
