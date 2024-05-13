using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;

public class DataHolder : MonoBehaviour {
    public static DataHolder s;

    public float cartLength;
    
    private void Awake() { 
        s = this;

        mergeDatas = new MergeData[mergeDataScriptables.Length];
        for (int i = 0; i < mergeDataScriptables.Length; i++) {
            mergeDatas[i] = mergeDataScriptables[i].GetMergeData();
        }
    }


    public Artifact[] artifacts;
    public Cart[] buildings;
    public CharacterDataScriptable[] characters;
    public EncounterTitle[] encounters;
    public PowerUpScriptable[] powerUps;
    public LevelArchetypeScriptable[] levelArchetypeScriptables;
    public GunModule[] swappableTier1Guns;
    public GunModule[] swappableTier2Guns;

    public MergeDataScriptable[] mergeDataScriptables;
    private MergeData[] mergeDatas;

    public Cart tier1GunCart;
    public Cart tier2GunCart;
    public Cart tier3GunCart;


    public PowerUpScriptable GetPowerUp(string powerUpUniqueName) {
        for (int i = 0; i < powerUps.Length; i++) {
            if (PreProcess(powerUps[i].name) == PreProcess(powerUpUniqueName)) {
                return powerUps[i];
            }
        }

        Debug.LogError($"Can't find power up {powerUpUniqueName}");
        return null;
    }

    public GameObject GetEncounter(string encounterUniqueName) {
        for (int i = 0; i < encounters.Length; i++) {
            if ("e_" + PreProcess(encounters[i].gameObject.name) == PreProcess(encounterUniqueName)) {
                return encounters[i].gameObject;
            }
        }

        Debug.LogError($"Can't find encounter {encounterUniqueName}");
        return null;
    }


    public Artifact GetArtifact(string artifactName, bool suppressWarning = false) {
        for (int i = 0; i < artifacts.Length; i++) {
            if (PreProcess(artifacts[i].uniqueName) == PreProcess(artifactName)) {
                return artifacts[i];
            }
        }

        if (!suppressWarning) {
            for (int i = 0; i < artifacts.Length; i++) {
                Debug.LogError(PreProcess(artifacts[i].uniqueName));
            }

            Debug.LogError($"Can't find artifact <{artifactName}>");
            PlayStateMaster.s.OpenMainMenu(); // bail
        }

        return null;
    }
    
    public Cart GetCart(string buildingName, bool suppressWarning = false) {
        for (int i = 0; i < buildings.Length; i++) {
            if (PreProcess(buildings[i].uniqueName) == PreProcess(buildingName)) {
                return buildings[i];
            }
        }
        
        // cant find building, maybe its a gun?
        var tier1Gun = GetTier1Gun(buildingName);
        if (tier1Gun != null) {
            return tier1GunCart;
        }
        
        var tier2Gun = GetTier2Gun(buildingName);

        if (tier2Gun != null) {
            return tier2GunCart;
        }

        if (!suppressWarning) {
            Debug.LogError($"Can't find building <{buildingName}>");
            var allBuildings = GetAllPossibleBuildingNames();
            for (int i = 0; i < allBuildings.Count; i++) {
                Debug.LogError(PreProcess(allBuildings[i]));
            }

            SettingsController.s.ResetTrainAndBail();
        }

        return null;
    }
    
    
    public GunModule GetTier1Gun(string gunName) {
        for (int i = 0; i < swappableTier1Guns.Length; i++) {
            if (PreProcess(swappableTier1Guns[i].gunUniqueName) == PreProcess(gunName)) {
                return swappableTier1Guns[i];
            }
        }
        
        return null;
    }
    public GunModule GetTier2Gun(string gunName) {
        for (int i = 0; i < swappableTier2Guns.Length; i++) {
            if (PreProcess(swappableTier2Guns[i].gunUniqueName) == PreProcess(gunName)) {
                return swappableTier2Guns[i];
            }
        }
        
        return null;
    }

    public CharacterData GetCharacter(string charName) {
        for (int i = 0; i < characters.Length; i++) {
            if (PreProcess(characters[i].myCharacter.uniqueName) == PreProcess(charName)) {
                return characters[i].myCharacter;
            }
        }

        Debug.LogError($"Can't find character {charName}");
        return null;
    }

    public static string PreProcess(string input) {
        return input.Replace(" ", "").ToLower();
    }


    public List<string> GetAllPossibleBuildingNames() {
        var buildingNames = new List<string>();
        buildingNames.Add("");
        for (int i = 0; i < buildings.Length; i++) {
            buildingNames.Add(buildings[i].uniqueName);
        }
        for (int i = 0; i < swappableTier1Guns.Length; i++) {
            buildingNames.Add(swappableTier1Guns[i].gunUniqueName);
        }
        for (int i = 0; i < swappableTier2Guns.Length; i++) {
            buildingNames.Add(swappableTier2Guns[i].gunUniqueName);
        }

        return buildingNames;
    }
    
    
    public List<string> GetAllPossibleArtifactNames() {
        var artifactNames = new List<string>();
        artifactNames.Add("");
        for (int i = 0; i < artifacts.Length; i++) {
            artifactNames.Add(artifacts[i].uniqueName);
        }
        return artifactNames;
    }


    public string GetMergeResult(string uniqueName1, string uniqueName2) {
        var inputStrings = new List<string>() { PreProcess(uniqueName1), PreProcess(uniqueName2) };
        inputStrings.Sort();

        for (int i = 0; i < mergeDatas.Length; i++) {
            if (mergeDatas[i].sources[0] == inputStrings[0] && mergeDatas[i].sources[1] == inputStrings[1]) {
                return mergeDatas[i].result;
            }
        }

        return null;
    }

    public bool IsLegalMergeResult(string result) {
        return result != null;
    }
}
