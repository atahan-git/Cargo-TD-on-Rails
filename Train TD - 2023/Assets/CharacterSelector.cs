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

    public GameObject carSelectorArea;
    public GameObject startTrainArea;

    public MiniGUI_DepartureChecklist departureChecklist;
    
    public bool isInCharSelect = false;

    public GateScript myGate;
    private void Start() {
        myGate.OnCanLeaveAndPressLeave.AddListener(CharSelectedAndLeave);
        carSelectorArea.SetActive(false);
        startTrainArea.SetActive(true);
        charSelectUI.SetActive(false);
    }

    public void CheckAndShowCharSelectionScreen() {
        PlayerWorldInteractionController.s.canSelect = true;
        if (!DataSaver.s.GetCurrentSave().isInARun) {
            charSelectUI.SetActive(true);
            startTrainArea.SetActive(false);
            carSelectorArea.SetActive(true);
            SetUpCharPanel();
            isInCharSelect = true;
            PlayerWorldInteractionController.s.canOnlySelectCharSelectStuff = true;
            LevelReferences.s.cartHealthParent.gameObject.SetActive(false);
            ShopStateController.s.mapOpenButton.interactable = false;
            CameraController.s.MoveToCharSelectArea();
            
            if (DataSaver.s.GetCurrentSave().metaProgress.castlesTraveled == 0) {
                SelectCharacter(DataHolder.s.characters[0].myCharacter);
                CharSelectedAndLeave();
            }
        } else {
            charSelectUI.SetActive(false);
            startTrainArea.SetActive(true);
            carSelectorArea.SetActive(false);
            isInCharSelect = false;
            ShopStateController.s.mapOpenButton.interactable = true;
        }
    }


    void CheckDepartureRequirements() {
        //departureChecklist.UpdateStatus(new []{false, false});
    }

    void SetUpCharPanel() {
        Train.s.gameObject.SetActive(false);
        
        var allChars = DataHolder.s.characters;

        selectedChar = allChars[0].myCharacter;

        var progress = DataSaver.s.GetCurrentSave().metaProgress;
        
        StarterTrainSelector.s.DrawSections();
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
        
        CheckDepartureRequirements();
    }

    public CharacterData selectedChar;
    public void SelectCharacter(CharacterData _data) {
        selectedChar = _data;
        CheckDepartureRequirements();
    }


    public void CharSelectedAndLeave() {
        DataSaver.s.GetCurrentSave().currentRun = new DataSaver.RunState(VersionDisplay.s.GetVersionNumber());
        DataSaver.s.GetCurrentSave().currentRun.currentAct = 1;
        DataSaver.s.GetCurrentSave().currentRun.SetCharacter(selectedChar);
        
        DataSaver.s.GetCurrentSave().metaProgress.bonusArtifact = "";

        DataSaver.s.SaveActiveGame();

        isInCharSelect = false;
        PlayStateMaster.s.FinishCharacterSelection();
    }

    public void CharSelectionCompleteAndScreenGotDark() {
        DataSaver.s.GetCurrentSave().isInARun = true;
        ShopStateController.s.mapOpenButton.interactable = true;
        PlayerWorldInteractionController.s.canOnlySelectCharSelectStuff = false;
        
        carSelectorArea.SetActive(false);
        startTrainArea.SetActive(true);
        Train.s.gameObject.SetActive(true);
        LevelReferences.s.cartHealthParent.gameObject.SetActive(true);
    }
}
