using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarrierEnemy : MonoBehaviour {
    public GameObject[] carriedThings = new GameObject[3];
    public bool[] canCarry = new bool[]{true,true,true};
    public DataSaver.TrainState.CartState[] carryAwards = new DataSaver.TrainState.CartState[3];
    public Transform awardSpawnPos;
    
    public int currentCarry;
    // Start is called before the first frame update
    void Start()
    {
        SetWhatIsBeingCarried();
    }


    void SetWhatIsBeingCarried() {
        var possibleCarries = new List<int>();

        for (int i = 0; i < canCarry.Length; i++) {
            if (canCarry[i]) {
                possibleCarries.Add(i);
            }
        }

        if (possibleCarries.Count > 0) {
            currentCarry = possibleCarries[Random.Range(0, possibleCarries.Count)];
        }

        for (int i = 0; i < carriedThings.Length; i++) {
            carriedThings[i].SetActive(i == currentCarry);
        }
    }


    public void AwardTheCarriedThingOnDeath() {
        var award = carryAwards[Random.Range(0, carryAwards.Length)];
        
        var awardCart = Instantiate(DataHolder.s.GetCart(award.uniqueName).gameObject, awardSpawnPos.position, awardSpawnPos.rotation).GetComponent<Cart>();

        awardCart.gameObject.AddComponent<RubbleFollowFloor>();
        
        Train.ApplyStateToCart(awardCart, award);
        awardCart.SetHoldingState(false);
    }
}
