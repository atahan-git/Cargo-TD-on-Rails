using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemHolderHoldEffect : MonoBehaviour
{
	public void SetPositions(Vector3 basePos, IPlayerHoldable item) {
		var itemPos = item.GetUITargetTransform().position;
		
		GetComponent<LineRenderer>().SetPositions(new []{basePos, itemPos});
	}
	    
}
