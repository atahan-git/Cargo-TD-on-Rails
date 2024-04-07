using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ArtifactsController : MonoBehaviour {

	public static ArtifactsController s;

	private void Awake() {
		s = this;
	}
	
	public List<Artifact> myArtifacts = new List<Artifact>();

	public void ModifyEnemy(EnemyHealth enemyHealth) {
		for (int i = 0; i < myArtifacts.Count; i++) {
			if(!myArtifacts[i].GetComponentInParent<Cart>().isDestroyed)
				myArtifacts[i].GetComponent<ActivateWhenEnemySpawns>()?.ModifyEnemy(enemyHealth);
		}
	}


	public void ArtifactsChanged() {
		GetArtifacts();
	}

	void GetArtifacts() {
		myArtifacts.Clear();

		for (int i = 0; i < Train.s.carts.Count; i++) {
			var artifact = Train.s.carts[i].GetComponentInChildren<Artifact>();
			if(artifact != null)
				myArtifacts.Add(artifact);
		}
		
	}
}




public abstract class ActivateWhenOnArtifactRow : MonoBehaviour {


	public void Arm() {
		_Arm();
	}

	protected abstract void _Arm();
    

	public void Disarm() {
		_Disarm();
	}
    
    
	protected abstract void _Disarm();
	
	public List<Cart> GetAllCarts() {
		var carts = new List<Cart>();
		carts.AddRange(Train.s.carts);
		carts.AddRange(ShopStateController.s.shopCarts);
		return carts;
	}
}

public abstract class ActivateWhenEnemySpawns : MonoBehaviour {

	public abstract void ModifyEnemy(EnemyHealth enemyHealth);
    
	
}