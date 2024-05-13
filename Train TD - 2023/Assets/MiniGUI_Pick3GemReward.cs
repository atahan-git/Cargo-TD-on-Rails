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
    public void SetUp(string uniqueName) {
        myItemUniqueName = uniqueName;

        var artifact = DataHolder.s.GetArtifact(myItemUniqueName);
        title.text = artifact.displayName;
        description.text = artifact.GetDescription();
        icon.sprite = artifact.mySprite;
    }


    public void Select() {
       var gem = Instantiate(DataHolder.s.GetArtifact(myItemUniqueName).gameObject, StopAndPick3RewardUIController.s.instantiatePos);
       VisualEffectsController.s.SmartInstantiate(LevelReferences.s.goodItemSpawnEffectPrefab, StopAndPick3RewardUIController.s.instantiatePos);
       
       
       gem.GetComponent<Rigidbody>().isKinematic = false;
       gem.GetComponent<Rigidbody>().useGravity = true;
       
       LevelReferences.s.combatHoldableThings.Add(gem.GetComponent<Artifact>());
       
       StopAndPick3RewardUIController.s.RewardWasPicked();
    }
}
