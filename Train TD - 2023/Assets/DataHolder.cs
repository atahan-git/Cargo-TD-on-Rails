using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DataHolder : MonoBehaviour {
    public static DataHolder s;

    public float cartLength;
    
    private void Awake() { 
        s = this;
    }


    public Artifact[] artifacts;
    public Cart[] buildings;
    public EnemyHolder[] enemies;
    public CharacterDataScriptable[] characters;
    public CityDataScriptable[] cities;
    public EncounterTitle[] encounters;
    public LevelDataScriptable[] levels;
    public PowerUpScriptable[] powerUps;


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
    
    public LevelData GetLevel(string levelUniqueName) {
        if (levelUniqueName.StartsWith("e_")) {
            return new LevelData() { levelName = levelUniqueName, isEncounter = true };
        }
        
        for (int i = 0; i < levels.Length; i++) {
            var data = levels[i].GetData();
            if (PreProcess(data.levelName) == PreProcess(levelUniqueName)) {
                return data;
            }
        }

        Debug.LogError($"Can't find level {levelUniqueName}");
        return null;
    }

    public CityDataScriptable GetCityScriptable(string cityUniqueName) {
        for (int i = 0; i < cities.Length; i++) {
            if (PreProcess(cities[i].cityData.uniqueName) == PreProcess(cityUniqueName)) {
                return cities[i];
            }
        }

        Debug.LogError($"Can't find city {cityUniqueName}");
        return null;
    }

    public Artifact GetArtifact(string artifactName) {
        for (int i = 0; i < artifacts.Length; i++) {
            if (PreProcess(artifacts[i].uniqueName) == PreProcess(artifactName)) {
                return artifacts[i];
            }
        }

        for (int i = 0; i < artifacts.Length; i++) {
            Debug.LogError(PreProcess(artifacts[i].uniqueName));
        }
        Debug.LogError($"Can't find artifact <{artifactName}>");
        PlayStateMaster.s.OpenMainMenu(); // bail
        
        return null;
    }
    
    public Cart GetCart(string buildingName) {
        for (int i = 0; i < buildings.Length; i++) {
            if (PreProcess(buildings[i].uniqueName) == PreProcess(buildingName)) {
                return buildings[i];
            }
        }

        Debug.LogError($"Can't find building <{buildingName}>");
        for (int i = 0; i < buildings.Length; i++) {
			Debug.LogError(PreProcess(buildings[i].uniqueName));
		}
        PlayStateMaster.s.OpenMainMenu(); // bail
        
        return null;
    }
    
    public GameObject GetEnemy(string enemyName) {
        for (int i = 0; i < enemies.Length; i++) {
            if (PreProcess(enemies[i].uniqueName) == PreProcess(enemyName)) {
                return enemies[i].data;
            }
        }

        Debug.LogError($"Can't find enemy {enemyName}");
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

    string PreProcess(string input) {
        return input.Replace(" ", "").ToLower();
    } 
}


[Serializable]
public class EnemyHolder {
    public string uniqueName = "unset";
    public GameObject data;
}