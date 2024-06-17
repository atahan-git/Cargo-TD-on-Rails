using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FirstTimeTutorialController : MonoBehaviour {
    public static FirstTimeTutorialController s;

    private DataSaver.TutorialProgress _progress => DataSaver.s.GetCurrentSave().tutorialProgress;
    
    
    public GameObject tutorialUI;

    public List<GameObject> activeHints = new List<GameObject>();

    private void Awake() {
        s = this;
        tutorialUI.SetActive(false);
    }


    public void OnEnterShop() {
        RemoveAllTutorialStuff();
        TutorialCheck();
    }
    
    public void TutorialCheck() {
    }
    

    public void ReDoTutorial() {
        //TutorialComplete();
        //DataSaver.s.GetCurrentSave().isInARun = false;
        DataSaver.s.GetCurrentSave().tutorialProgress = new DataSaver.TutorialProgress();
        //DataSaver.s.GetCurrentSave().metaProgress = new DataSaver.MetaProgress();
        //MiniGUI_DisableTutorial.SetVal(true);
        //ShopStateController.s.BackToMainMenu();
        
        DataSaver.s.SaveActiveGame();
        SceneLoader.s.ForceReloadScene();
    }
    

    public InputActionReference skip;

    IEnumerator WaitForSecondsSmart(float toWait) {
        yield return null;
        var curTimer = 0f;
        while (curTimer <= toWait) {
            curTimer += Time.deltaTime;
            if(skip.action.WasPerformedThisFrame())
                break;
            yield return null;
        }
        yield return null;
    }

    
    void ClearActiveHints() {
        for (int i = 0; i < activeHints.Count; i++) {
            if(activeHints[i] != null)
                Destroy(activeHints[i].gameObject);
        }
        
        activeHints.Clear();
    }
    
    public void OnEnterCombat() {
        ClearActiveHints();
    }
    
    public void OnFinishCombat(bool realCombat) {
        ClearActiveHints();
        
        if (!MiniGUI_DisableTutorial.IsTutorialActive())
            return;
    }

    public void ReloadHintShown() {
        //_progress.reloadHint = true;
    }

    public void RemoveAllTutorialStuff() {
        tutorialUI.SetActive(false);
        ClearActiveHints();
    }
}
