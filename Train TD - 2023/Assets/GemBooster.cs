using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemBooster : MonoBehaviour,  IResetState
{
	
	public int baseRange = 1;
	public int rangeBoost = 0;
	public Color myColor = Color.green;

	public GameObject artifactChunk;
	
	public void ModifyStats(int range, float value) {
		rangeBoost += range;
	}

	public int GetRange() {
		return Mathf.Min(Train.s.carts.Count, baseRange + rangeBoost );
	}

	public Color GetColor() {
		return myColor;
	}

	public void ResetState() {
		rangeBoost = 0;
		var artifact = GetComponentInParent<Cart>().GetComponentInChildren<Artifact>();
		if (artifact) {
			artifact.ApplyToTarget.AddListener(AddChunk);
		}
		parents.Clear();
	}

	private List<Transform> parents =new List<Transform>();
	public void AddChunk(Cart target) {
		/*var artifact = GetComponentInParent<Cart>().GetComponentInChildren<Artifact>();
		var material = artifact.GetComponentInChildren<MeshRenderer>().material;

		var targetTransform = target.artifactChunkTransform;
		var chunk = Instantiate(artifactChunk, targetTransform);
		chunk.GetComponentInChildren<MeshRenderer>().material = material;
		chunk.transform.rotation = Random.rotation;
		parents.Add(targetTransform);*/

		Invoke(nameof(SetPos), 0.01f);
	}

	void SetPos() {
		for (int j = 0; j < parents.Count; j++) {
			var parent = parents[j];
			
			var childCount =parent.childCount;
			var spacing = Mathf.Min(childCount / 6f,0.1f);

			var offset = -(((childCount-1) / 2f) * spacing);
		

			for (int i = 0; i < parent.childCount; i++) {
				var child = parent.GetChild(i);
				child.transform.localPosition = Vector3.forward* ((i*spacing) + offset);
			}
		}
	}
}
