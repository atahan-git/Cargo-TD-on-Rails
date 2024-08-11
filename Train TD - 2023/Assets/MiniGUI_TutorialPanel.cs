using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGUI_TutorialPanel : MonoBehaviour
{
    void Update()
    {
        if (PlayerWorldInteractionController.s.hideTutorial.action.WasPerformedThisFrame()) {
            HidePanel();
        }
    }


    public void HidePanel() {
        gameObject.SetActive(false);
    }
}
