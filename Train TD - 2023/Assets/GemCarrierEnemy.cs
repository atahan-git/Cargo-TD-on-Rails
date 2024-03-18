using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemCarrierEnemy : MonoBehaviour {
    
    public NumberWithWeights[] carriedThingsSpawnWeights = new NumberWithWeights[3];
    public GameObject[] carriedThings = new GameObject[3];
    public bool[] canCarry = new bool[]{true,true,true};
    public DataSaver.TrainState.ArtifactState[] carryAwards = new DataSaver.TrainState.ArtifactState[3];
    public Transform awardSpawnPos;
    
    public int currentCarry;
    // Start is called before the first frame update
    void Start()
    {
        SetWhatIsBeingCarried();
    }


    void SetWhatIsBeingCarried() {
        currentCarry = NumberWithWeights.WeightedRandomRoll(carriedThingsSpawnWeights);
        
        for (int i = 0; i < carriedThings.Length; i++) {
            carriedThings[i].SetActive(i == currentCarry);
            if (i == currentCarry) {
                carriedThings[i].GetComponent<IApplyToEnemyWithGem>().ApplyToEnemyWithGem(GetComponent<EnemyInSwarm>());
            }
        }
    }


    public void AwardTheCarriedThingOnDeath() {
        var award = carryAwards[Random.Range(0, carryAwards.Length)];
        
        var awardArtifact = Instantiate(DataHolder.s.GetArtifact(award.uniqueName).gameObject, awardSpawnPos.position, awardSpawnPos.rotation).GetComponent<Artifact>();

        awardArtifact.gameObject.AddComponent<RubbleFollowFloor>();
        
        LevelReferences.s.combatHoldableThings.Add(awardArtifact);
        
        Train.ApplyStateToArtifact(awardArtifact, award);
        awardArtifact.SetHoldingState(false);
        
        awardArtifact.GetComponent<Rigidbody>().AddForce(Vector3.up*Random.Range(2000,2500) + Vector3.left * Random.Range(500,1000) * (Random.value > 0.5f ? 1 : -1));
        awardArtifact.GetComponent<Rigidbody>().AddTorque(Vector3.one*2000);
    }
}


interface IApplyToEnemyWithGem {
    void ApplyToEnemyWithGem(EnemyInSwarm enemy);
}