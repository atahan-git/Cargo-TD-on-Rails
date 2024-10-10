using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledObject : MonoBehaviour {

	public int myId = -1;
	public ObjectPool myPool;

	public bool isActive = false;

	[SerializeField]
	float _lifetime = -1f; //if a value bigger than zero will auto disable after that time
	public float lifeTime{
		get{
			return _lifetime;
		}
		set{
			if (_lifetime != value) {
				_lifetime = value;
				LifetimeChangeCheck ();
			}
		}
	}

	//These two should only be called from server side
	/// <summary>
	/// DONT CALLS THIS. this is only for internal ObjectPool use. Use ObjectPool.Spawn() instead
	/// </summary>
	public void EnableObject (){
		ResetValues();
		gameObject.SetActive(true);
		isActive = true;
		if (IsInvoking (nameof(DestroyPooledObject))) {
			CancelInvoke (nameof(DestroyPooledObject));
		};
		if (lifeTime > 0f)
			Invoke (nameof(DestroyPooledObject), lifeTime);
		myPool.ActiveObjects += 1;
	}


	void LifetimeChangeCheck () {
		if (IsInvoking (nameof(DestroyPooledObject))) {
			CancelInvoke (nameof(DestroyPooledObject));
		};
		if(lifeTime > 0)
			Invoke (nameof(DestroyPooledObject), lifeTime);
	}


	//only server side
	/// <summary>
	/// DONT CALLS THIS. this is only for internal ObjectPool use. Use PooledObject.DestroyPooledObject() instead
	/// </summary>
	public void DisableObject (){
		gameObject.SetActive (false);
		isActive = false;
		myPool.ActiveObjects -= 1;
		CancelInvoke (nameof(DisableObject));
	}

	public void DestroyPooledObject (){
		if (GetComponent<TrainTerrainData>()) {
			GetComponent<TrainTerrainData>()._DestroyPooledObject();
		}
		myPool.DestroyPooledObject (myId);
	}

	void ResetValues () {
		var trail = GetComponentInChildren<SmartTrail>();
		if (trail != null) {
			trail.Reset();
		}
		foreach (ParticleSystem prt in GetComponentsInChildren<ParticleSystem>()) {
			if (prt != null) {
				prt.Clear ();
				prt.Play ();
			}
		}

		var moveWithGlobe = GetComponent<MoveWithGlobalMovement>();
		if (moveWithGlobe) {
			moveWithGlobe.currentSpeedPercent = moveWithGlobe.startSpeedPercent;
		}

		if (GetComponent<Rigidbody> () != null) {
			GetComponent<Rigidbody> ().velocity = Vector3.zero;
			GetComponent<Rigidbody> ().angularVelocity = Vector3.zero;
		}

		if (GetComponent<RandomPitchAtStart>() != null) {
			GetComponent<RandomPitchAtStart>().Play();
		}

		if (GetComponent<ResetObjPosWhenPooledObjReset>() != null) {
			GetComponent<ResetObjPosWhenPooledObjReset>().ResetObject();
		}
	}
}
