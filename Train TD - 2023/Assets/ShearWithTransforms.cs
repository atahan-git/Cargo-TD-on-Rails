using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ShearWithTransforms : MonoBehaviour {


	[Button]
	public static void ResizeObject (GameObject obj, float objWidth, float objHeight) {
		if (obj.transform.forward.y > 0) {
			obj.transform.Rotate(0,180,0);
			obj.transform.GetChild(0).transform.localScale = new Vector3(-1, 1, 1);
		}
		
		ParallelShear (obj, objWidth, objHeight);
	}
	//-------------------------------------------------------- SHEAR STUFF

	/*
	 * 									C
	 * 							O		|
	 * 					_		| -		|
	 * 			_				|    -	|
	 * 		A---------------------------B
	 * 							|
	 * 							D
	 */


	public static void ParallelShear (GameObject obj, float sizeX, float sizeY) {
		var parentTransform = obj.transform.parent;
		
		Vector3 a = obj.transform.position;
		Vector3 b = obj.transform.position + (-obj.transform.forward * sizeX);

		Vector3 c = b + (obj.transform.up * sizeY);

		//Debug.DrawLine (a, b, Color.red);
		//Debug.DrawLine (b, c, Color.green);

		Vector3 d = new Vector3 (c.x, a.y, c.z);

		var firstp = new GameObject();
		var secondp = new GameObject();
		firstp.transform.parent = parentTransform;
		secondp.transform.parent = parentTransform;
		firstp.name = "Parallel Correction";
		secondp.name = obj.name + " Size Correction";

		firstp.transform.position = a;
		secondp.transform.position = a;
		firstp.transform.LookAt (c, obj.transform.up);
		secondp.transform.LookAt (new Vector3 (c.x, a.y, c.z), Vector3.up);
		//firstp.transform.rotation = Quaternion.LookRotation (a - c);
		firstp.transform.parent = secondp.transform;

		//math
		float bc = Vector3.Distance (b, c);
		float ac = Vector3.Distance (a, c);
		float oc = (bc * bc) / ac;

		Vector3 o = Vector3.Lerp (c, a, oc / ac);

		float cd = Vector3.Distance (c, d);
		float similarityMultiplier = cd / oc;
		float ad = Vector3.Distance (a, d);
		float og = ad / similarityMultiplier;

		float ob = Vector3.Distance (o, b);
		Vector3 g = Vector3.Lerp (o, b, og / ob);

		float gb = Vector3.Distance (g, b);


		Vector3 m = new Vector3 (d.x, b.y, d.z);
		Vector3 e = new Vector3 (b.x, a.y, b.z);
		float ae = Vector3.Distance (a, e);
		float bm = Vector3.Distance (b, m);
		float gd = Vector3.Distance (g, d);
		float gm = Vector3.Distance (g, m);

		//actual doing it

		firstp.transform.localScale = new Vector3 (1, og / ob, 1);
		//Debug.DrawLine (o, g, Color.blue);
		//Debug.DrawLine (g, b, Color.white);

		Quaternion tempRot = obj.transform.rotation;
		obj.transform.SetParent (firstp.transform, false);
		obj.transform.localPosition = Vector3.zero;
		obj.transform.rotation = tempRot;
		secondp.transform.localScale = new Vector3 (1, ((gd - gm) / gd), (ae / (ae - bm)));

		float _gc = ((gd - gm) / gd) * Vector3.Distance (g, c);

		obj.transform.localScale = new Vector3 (1, sizeY / _gc, 1);
	}
}
