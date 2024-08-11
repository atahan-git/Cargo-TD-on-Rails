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
		Low,
		damageNumbers
	}

	
	private void Update() {
		mediumEffectCount -= 10*Time.deltaTime;
		mediumEffectCount = Mathf.Clamp(mediumEffectCount, 0, 5);
	}

	private void LateUpdate() {
		transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(0);
		transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(0);
	}

	private float mediumEffectCount = 0;
	public GameObject SmartInstantiate(GameObject myObj, Vector3 position, Quaternion rotation, EffectPriority priority = EffectPriority.Always) {
		if (priority == EffectPriority.Low || priority==EffectPriority.damageNumbers)
			return null;
		if (priority == EffectPriority.Medium) {
			mediumEffectCount += 1;
			if (mediumEffectCount > 5) {
				return null;
			}
		}
		
		var obj = Instantiate(myObj, position, rotation, transform);
		PostProcessingOnEffects(obj);
		return obj;
	}
	
	public GameObject SmartInstantiate(GameObject myObj, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent, EffectPriority priority = EffectPriority.Always) {
		if (priority == EffectPriority.Low || priority==EffectPriority.damageNumbers)
			return null;
		if (priority == EffectPriority.Medium) {
			mediumEffectCount += 1;
			if (mediumEffectCount > 5) {
				return null;
			}
		}
		
		var obj = Instantiate(myObj, position, rotation, parent);
		obj.transform.localScale = scale;
		PostProcessingOnEffects(obj);
		return obj;
	}
	
	public GameObject SmartInstantiate(GameObject myObj, Transform parent, EffectPriority priority = EffectPriority.Always) {
		if (priority == EffectPriority.Low || priority==EffectPriority.damageNumbers)
			return null;
		
		var obj = Instantiate(myObj, parent.position, parent.rotation, parent);
		PostProcessingOnEffects(obj);
		return obj;
	}
	
	public GameObject SmartInstantiate(GameObject myObj, Transform parent, Vector3 position, Quaternion rotation, EffectPriority priority = EffectPriority.Always) {
		var obj = Instantiate(myObj,position, rotation, parent);
		PostProcessingOnEffects(obj);
		return obj;
	}


	void PostProcessingOnEffects(GameObject effect) {
		/*var lights = effect.GetComponentsInChildren<Light>();
		foreach (var light in lights) {
			light.gameObject.SetActive(false);
		}*/
	}
}
