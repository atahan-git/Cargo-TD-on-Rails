using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ObjectPool : MonoBehaviour {

	public bool autoExpand = true; //dont change this at runtime
	public GameObject myObject;
	public int poolSize = 32;
	[Space]
	public int ExistingObjects;
	public int ActiveObjects;

	public bool autoSetUp = true;
	private bool setUpAtLeastOnce = false;

	void Awake (){
		if (autoSetUp && !setUpAtLeastOnce) {
			ResetPool();
		}
	}


	void ResetPool() {
		if (myObject.GetComponent<PooledObject> () == null)
			myObject.AddComponent<PooledObject> ();

		myObject.GetComponent<PooledObject> ().myPool = this;

		setUpAtLeastOnce = true;
		
		for (int i = transform.childCount-1; i >=0 ; i--) {
			Destroy(transform.GetChild(i).gameObject);
		}

		ExistingObjects = 0;
		ActiveObjects = 0;

		SetUp(poolSize);
	}

	GameObject[] objs;
	Queue<int> activeIds = new Queue<int>();

	public GameObject[] GetAllObjs() {
		return objs;
	}

	public bool RePopulateWithNewObject(GameObject obj) {
		if (myObject != obj || !setUpAtLeastOnce) {
			myObject = obj;
			ResetPool();
			return true;
		}

		return false;
	}

	void SetUp (int poolsize){
		objs = new GameObject[poolsize];
		for (int i = 0; i < poolsize; i++) {
			GameObject inst = (GameObject)Instantiate (myObject, transform);
			ExistingObjects += 1;
			inst.GetComponent<PooledObject> ().myId = i;
			inst.GetComponent<PooledObject> ().DisableObject ();
			ActiveObjects += 1;
			objs[i] = (inst);
		}
	}

	void FillArrayWithObjects () {
		for (int i = 0; i < objs.Length; i++) {
			if (objs[i] == null) {
				GameObject inst = (GameObject)Instantiate(myObject, transform);
				ExistingObjects += 1;
				inst.GetComponent<PooledObject>().myId = i;
				inst.GetComponent<PooledObject>().DisableObject();
				ActiveObjects += 1;
				objs[i] = (inst);
			}
		}
	}

	public void ExpandPoolToSize(int size) {
		size = Mathf.NextPowerOfTwo(size);
		if (size < ExistingObjects) {
			return;
		}

		poolSize = size;
		
		if (!setUpAtLeastOnce) {
			ResetPool();
			return;
		}
		
		

		GameObject[] temp = objs;
		objs = new GameObject[size];
		temp.CopyTo(objs, 0);
		
		FillArrayWithObjects();
	}


	GameObject _Spawn (Vector3 pos, Quaternion rot){
		#if UNITY_EDITOR
		if(!Application.isPlaying){
			GameObject inst = (GameObject)Instantiate(myObject, transform);
			inst.transform.position = pos;
			inst.transform.rotation = rot;
			return inst;
		}
		#endif
		
		for (int i = 0; i < objs.Length; i++) {
			if (!objs [i].GetComponent<PooledObject>().isActive) {

				objs [i].transform.position = pos;
				objs [i].transform.rotation = rot;
				objs [i].GetComponent<PooledObject> ().EnableObject ();

				if (!autoExpand)
					activeIds.Enqueue (i);
				
				return objs [i];
			}
		}
		print ($"{gameObject.name} - Not enough pooled objects detected - {objs.Length} -> {objs.Length*2}");

		//there is no free object left
		if (autoExpand) {
			GameObject[] temp = objs;
			objs = new GameObject[objs.Length*2];
			temp.CopyTo(objs, 0);
			GameObject inst = (GameObject)Instantiate (myObject, transform);
			ExistingObjects += 1;
			inst.transform.position = pos;
			inst.transform.rotation = rot;
			inst.GetComponent<PooledObject>().EnableObject();

			objs[temp.Length] =  (inst);
			inst.GetComponent<PooledObject> ().myId = temp.Length;

			StopAllCoroutines();
			FillArrayWithObjects();
			poolSize = objs.Length;
			return objs [temp.Length];
		} else {
			int toReuse = activeIds.Dequeue ();
			activeIds.Enqueue (toReuse);

			objs [toReuse].transform.position = pos;
			objs [toReuse].transform.rotation = rot;
			objs [toReuse].GetComponent<PooledObject> ().EnableObject ();
			ActiveObjects -= 1;
			return objs [toReuse];
		}
	}





	public GameObject Spawn(Vector3 pos, Quaternion rot){
		return _Spawn (pos, rot);
	}
		
		
	public GameObject Spawn (Vector3 pos){
		return Spawn (pos, Quaternion.identity);
	}
		

	public GameObject Spawn (float x, float y, float z){
		return Spawn (new Vector3 (x, y, z));
	}

	/*public GameObject Spawn (){
		return Spawn (myObject.transform.position, myObject.transform.rotation);
	}*/



	public void DestroyPooledObject (int id){
		if (objs[id] != null) {
			objs[id].GetComponent<PooledObject>().DisableObject();
		} else {
			Debug.LogError("Pooled object with wrong id detected");
		}
	}
}