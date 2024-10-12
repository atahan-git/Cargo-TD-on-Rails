using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyGunSlot : MonoBehaviour
{
	public enum GunSlotType {
		Any=0, NormalOnly=1, UniqueOnly=2, EliteOnly=3
	}

	public GunSlotType myType = GunSlotType.Any;
}
