using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarrierEnemy : MonoBehaviour {
    public GameObject[] carriedThings = new GameObject[3];
    public DataSaver.TrainState.CartState[] carryAwards = new DataSaver.TrainState.CartState[3];
    public Transform awardSpawnPos;
    
    public int currentCarry;

    public void SetWhatIsBeingCarried(string uniqueName) {
        for (int i = 0; i < carryAwards.Length; i++) {
            if (carryAwards[i].uniqueName == uniqueName) {
                currentCarry = i;
            }
        }

        for (int i = 0; i < carriedThings.Length; i++) {
            carriedThings[i].SetActive(i == currentCarry);
        }
    }


    public void AwardTheCarriedThingOnDeath() {
        var award = carryAwards[Random.Range(0, carryAwards.Length)];
        
        var awardCart = Instantiate(DataHolder.s.GetCart(award.uniqueName).gameObject, awardSpawnPos.position, awardSpawnPos.rotation).GetComponent<Cart>();

        awardCart.gameObject.AddComponent<RubbleFollowFloor>();
        
        LevelReferences.s.combatHoldableThings.Add(awardCart);
        
        Train.ApplyStateToCart(awardCart, award);
        awardCart.SetHoldingState(false);
        
        awardCart.GetComponent<Rigidbody>().AddForce(Vector3.up*Random.Range(2000,2500) + Vector3.left * Random.Range(500,1000) * (Random.value > 0.5f ? 1 : -1));
        awardCart.GetComponent<Rigidbody>().AddTorque(Vector3.one*2000);
    }
}
