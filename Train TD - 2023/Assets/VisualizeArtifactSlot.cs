using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualizeArtifactSlot : MonoBehaviour
{
	private void Start() {
		SetState(false);
		transform.GetChild(0).GetChild(1).gameObject.SetActive(GetComponentInParent<Cart>().canAcceptComponentArtifact);
	}

	public void SetState(bool isActive) {
		transform.GetChild(0).gameObject.SetActive(isActive);
	}
}
