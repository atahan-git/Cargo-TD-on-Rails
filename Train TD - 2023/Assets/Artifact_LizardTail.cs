using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact_LizardTail : MonoBehaviour, IChangeStateToEntireTrain
{
	public void ChangeStateToEntireTrain(List<Cart> carts) {
		Train.s.currentAffectors.lizardTail = this;
	}

	public void TailUp() {
		for (int i = 0; i < Train.s.carts.Count; i++) {
			Train.s.carts[i].GetHealthModule().RepairChunk(1000);
		}

		Train.s.currentAffectors.lizardTail = null;
		Destroy(gameObject);
	}
}
