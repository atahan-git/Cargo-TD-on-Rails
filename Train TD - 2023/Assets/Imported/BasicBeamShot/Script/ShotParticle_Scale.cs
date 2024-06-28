using UnityEngine;
using System.Collections;

public class ShotParticle_Scale : MonoBehaviour {

	private LineRenderer LR;
	private float width;
	private float length;
	private float time;
	private Vector3 forward;
	private float scale;

	// Use this for initialization
	void Start () {
		LR = transform.GetComponent<LineRenderer>();
		scale = GetComponentInParent<BeamParam>().Scale;
		scale *= 10;
		width = scale/3f;
		length = 0.0f;
		time = 0.0f;
		forward = transform.forward;

		var parent = transform.parent;
		Quaternion ParentQua = parent.rotation;
		//Vector3 V = ParentQua * forward;
		Vector3 V = parent.forward;

		LR.SetPosition(0, Vector3.zero);
		var otherPos = parent.position + V * parent.localScale.z * length;
		LR.SetPosition(1, parent.InverseTransformPoint(otherPos));
		LR.SetWidth(parent.localScale.x * width,parent.localScale.x * width);
	}
	
	// Update is called once per frame
	void Update () {
		Quaternion ParentQua = transform.parent.rotation;
		//Vector3 V = ParentQua * forward;
		var parent = transform.parent;
		Vector3 V = parent.forward;
		
		LR.SetPosition(0, Vector3.zero);
		var otherPos = parent.position + V * parent.localScale.z * length;
		LR.SetPosition(1, parent.InverseTransformPoint(otherPos));
		LR.SetWidth(parent.localScale.x * width,parent.localScale.x * width);

		width = Mathf.Lerp(width,0,time*time);
		length += 1.5f * Time.deltaTime * scale;

		time += Time.deltaTime;
	}
}
