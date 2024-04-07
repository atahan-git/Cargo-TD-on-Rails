using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeCarrierEnemy : MonoBehaviour
{
    
    public Transform awardSpawnPos;

    public void AwardTheCarriedThingOnDeath() {
        var awardMergeItem = Instantiate(LevelReferences.s.mergeGemItem, awardSpawnPos.position, awardSpawnPos.rotation).GetComponent<MergeItem>();

        awardMergeItem.gameObject.AddComponent<RubbleFollowFloor>();
        
        LevelReferences.s.combatHoldableThings.Add(awardMergeItem);
        
        awardMergeItem.SetHoldingState(false);
        
        awardMergeItem.GetComponent<Rigidbody>().AddForce(Vector3.up*Random.Range(2000,2500) + Vector3.left * Random.Range(500,1000) * (Random.value > 0.5f ? 1 : -1));
        awardMergeItem.GetComponent<Rigidbody>().AddTorque(Vector3.one*2000);
    }
}
