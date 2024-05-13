using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_ArtifactInfoCard : MonoBehaviour
{
    public Image icon;
    public TMP_Text moduleName;
    public TMP_Text moduleDescription;

    public void SetUp(Artifact artifact) {
        var posFollower = GetComponent<MiniGUI_InfoCardFollowPositionLogic>();
        
        icon.sprite = artifact.mySprite;
        moduleName.text = artifact.displayName;

        moduleDescription.text = artifact.GetDescription();
        
        posFollower.SetUp(artifact.GetUITargetTransform());
        Show();
    }

    public void Show() {
        GetComponent<MiniGUI_InfoCardFollowPositionLogic>().Show();
    }

    public void Hide() {
        GetComponent<MiniGUI_InfoCardFollowPositionLogic>().Hide();
    }
}
