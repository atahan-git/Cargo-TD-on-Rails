using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_Pick3GemReward : MonoBehaviour {

    public TMP_Text title;
    public TMP_Text description;
    public Image icon;

    public string myItemUniqueName;
    public bool isBigGem = false;
    public void SetUp(string uniqueName, bool _isBigGem) {
        myItemUniqueName = uniqueName;
        isBigGem = _isBigGem;

        var artifact = DataHolder.s.GetArtifact(myItemUniqueName);
        title.text = artifact.displayName;
        description.text = artifact.GetDescription();
        icon.sprite = artifact.mySprite;
    }


    public void Select() {
       MakeGem();

       if (isBigGem && Train.s.currentAffectors.dupeGems) {
	       MakeGem();
       }
       
       StopAndPick3RewardUIController.s.RewardWasPicked();
    }

    void MakeGem() {
	    var gem = Instantiate(DataHolder.s.GetArtifact(myItemUniqueName).gameObject, StopAndPick3RewardUIController.s.GetRewardPos(), StopAndPick3RewardUIController.s.GetRewardRotation());
	    Instantiate(LevelReferences.s.goodItemSpawnEffectPrefab, StopAndPick3RewardUIController.s.GetRewardPos(), StopAndPick3RewardUIController.s.GetRewardRotation());
       
	    var rg = gem.GetComponent<Rigidbody>();
	    rg.isKinematic = false;
	    rg.useGravity = true;
	    rg.velocity = Train.s.GetTrainForward() * LevelReferences.s.speed;
	    rg.AddForce(StopAndPick3RewardUIController.s.GetUpForce());
       
	    LevelReferences.s.combatHoldableThings.Add(gem.GetComponent<Artifact>());
    }
}
