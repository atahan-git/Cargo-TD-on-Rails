using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeItem : MonoBehaviour, IPlayerHoldable {

	public Cart myPrevCart;
	public Cart myNextCart;
	public Transform GetUITargetTransform() {
		return transform;
	}

	public void SetHoldingState(bool state) {
		if (state) {
			GetComponent<Rigidbody>().isKinematic = true;
			GetComponent<Rigidbody>().useGravity = false;
		} else {
			GetComponent<Rigidbody>().isKinematic = false;
			GetComponent<Rigidbody>().useGravity = true;
		}
	}

	public bool CanDrag() {
		return true;
	}
	private DroneRepairController _holder;
	public DroneRepairController GetHoldingDrone() {
		return _holder;
	}

	public void SetHoldingDrone(DroneRepairController holder) {
		_holder = holder;
	}
}
