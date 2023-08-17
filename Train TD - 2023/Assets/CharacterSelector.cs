using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterSelector : MonoBehaviour {
    public static CharacterSelector s;

    private void Awake() {
        s = this;
    }

    public GameObject charSelectUI;
    public Transform charsParent;
    public GameObject charPanelPrefab;

    public GameObject carSelectorArea;
    public GameObject startTrainArea;

    public MiniGUI_DepartureChecklist departureChecklist;
    
    public bool isInCharSelect = false;
    public void CheckAndShowCharSelectionScreen() {
        if (!DataSaver.s.GetCurrentSave().isInARun) {
            charSelectUI.SetActive(true);
            startTrainArea.SetActive(false);
            carSelectorArea.SetActive(true);
            SetUpCharPanel();
            isInCharSelect = true;
        } else {
            charSelectUI.SetActive(false);
            startTrainArea.SetActive(true);
            carSelectorArea.SetActive(false);
            isInCharSelect = false;
        }
    }


    void CheckDepartureRequirements() {
        departureChecklist.UpdateStatus(new []{false, false});
    }

    void SetUpCharPanel() {
        charsParent.DeleteAllChildren();
        var allChars = DataHolder.s.characters;
        for (int i = 0; i < allChars.Length; i++) {
            var panel = Instantiate(charPanelPrefab, charsParent).GetComponent<MiniGUI_CharSelectPanel>();
            panel.Setup(allChars[i].myCharacter, !XPProgressionController.s.IsCharacterUnlocked(i), i==0);
        }

        var progress = DataSaver.s.GetCurrentSave().xpProgress;
        /*if (progress.unlockedStarterArtifacts.Count == 0) {
            progress.unlockedStarterArtifacts.Add("starter_artifact");
        }
        
        for (int i = 0; i < progress.unlockedStarterArtifacts.Count; i++) {
            var artifact = Instantiate(DataHolder.s.GetArtifact(progress.unlockedStarterArtifacts[i]).gameObject, starterArtifactsParent).GetComponent<Artifact>();
            artifact.gameObject.AddComponent<RectTransform>();
            artifact.worldPart.gameObject.SetActive(false);
            artifact.uiPart.gameObject.SetActive(true);
        }

        
        selectedArtifact = starterArtifactsParent.GetChild(1).GetComponent<Artifact>();
        var newMarker = Instantiate(startingArtifactSelectedMarker, selectedArtifact.transform);
        Destroy(startingArtifactSelectedMarker);
        startingArtifactSelectedMarker = newMarker;
        starterWinWithItToGetMore.transform.SetParent(null);
        starterWinWithItToGetMore.transform.SetParent(starterArtifactsParent);

        if (progress.bonusArtifact != null && progress.bonusArtifact.Length > 0) {
            bonusArtifact = Instantiate(DataHolder.s.GetArtifact(progress.bonusArtifact).gameObject, bonusArtifactParent).GetComponent<Artifact>();
            bonusArtifact.gameObject.AddComponent<RectTransform>();
            bonusArtifact.worldPart.gameObject.SetActive(false);
            bonusArtifact.uiPart.gameObject.SetActive(true);
            bonusArtifactToggle.isOn = true;
            bonusArtifactToggle.interactable = true;
            bonusArtifactEmpty.transform.SetParent(null);
        } else {
            bonusArtifactEmpty.transform.SetParent(bonusArtifactParent);
            bonusArtifactToggle.isOn = false;
            bonusArtifactToggle.interactable = false;
        }*/
    }

    public void SelectCharacter(CharacterData _data) {
        DataSaver.s.GetCurrentSave().currentRun = new DataSaver.RunState(VersionDisplay.s.GetVersionNumber());
        DataSaver.s.GetCurrentSave().currentRun.currentAct = 1;
        DataSaver.s.GetCurrentSave().currentRun.SetCharacter(_data);
        DataSaver.s.GetCurrentSave().isInARun = true;

        
        DataSaver.s.GetCurrentSave().xpProgress.bonusArtifact = "";
            
            
        DataSaver.s.SaveActiveGame();

        isInCharSelect = true;
        PlayStateMaster.s.FinishCharacterSelection();
    }

    public void CharSelectionAndWorldGenerationComplete() {
        DataSaver.s.GetCurrentSave().isInARun = true;
    }
}
