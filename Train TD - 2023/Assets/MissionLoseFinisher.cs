using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public class MissionLoseFinisher : MonoBehaviour {
    public static MissionLoseFinisher s;

    private void Awake() {
        s = this;
    }

    public MonoBehaviour[] scriptsToDisable;
    public GameObject[] gameObjectsToDisable;
    
    public GameObject loseUI;

    public TMP_Text tipText;
    public TMP_Text loseReasonText;

    public string[] loseTips;

    public bool isMissionLost = false;

    public GameObject loseContinueButton;

    public enum MissionLoseReason {
        noEngine, noMysteryCargo, abandon
    }
    
    public void MissionLost(MissionLoseReason loseReason) {
        if (isMissionLost)
            return;

        tipText.text = loseTips[Random.Range(0, loseTips.Length)];

        switch (loseReason) {
            case MissionLoseReason.noEngine:
                loseReasonText.text = "You cannot continue further without your main engine.";
                break;
            case MissionLoseReason.noMysteryCargo:
                loseReasonText.text = "Your mission is useless without the mysterious cargo.";
                break;
            case MissionLoseReason.abandon:
                loseReasonText.text = "You abandoned your mission.";
                break;
            default:
                loseReasonText.text = "You cannot continue for unknown reasons (the programmer needs to fill this in!.";
                Debug.Log($"Unknown mission lose reason {loseReason}");
                break;
        }


        isMissionLost = true;
        PlayStateMaster.s.FinishCombat(true);

        Time.timeScale = 0;
        Pauser.s.isPaused = true;
        
        for (int i = 0; i < scriptsToDisable.Length; i++) {
            scriptsToDisable[i].enabled = false;
        }

        PlayerWorldInteractionController.s.canSelect = false;
		
        for (int i = 0; i < gameObjectsToDisable.Length; i++) {
            gameObjectsToDisable[i].SetActive(false);
        }
        
        loseUI.SetActive(true);
        
        
        var allArtifacts = ArtifactsController.s.myArtifacts;

        var eligibleBossArtifacts = new List<Artifact>();
        for (int i = 1; i < allArtifacts.Count; i++) {
            if (allArtifacts[i].myRarity == UpgradesController.CartRarity.boss) {
                eligibleBossArtifacts.Add(allArtifacts[i]);
            }
        }

        if (eligibleBossArtifacts.Count > 0) {
            DataSaver.s.GetCurrentSave().metaProgress.bonusArtifact = eligibleBossArtifacts[Random.Range(0, eligibleBossArtifacts.Count)].uniqueName;
        }

        DataSaver.s.GetCurrentSave().isInARun = false;
        
        DataSaver.s.SaveActiveGame();
        
        
        var myChar = DataSaver.s.GetCurrentSave().currentRun.character;
        AnalyticsResult analyticsResult = Analytics.CustomEvent(
            "LevelLost",
            new Dictionary<string, object> {
                { "Level", PlayStateMaster.s.currentLevel.levelName },
                { "distance", Mathf.RoundToInt(SpeedController.s.currentDistance / 10) *10},
                { "time", Mathf.RoundToInt(SpeedController.s.currentTime/10) * 10},
                
                {"character", myChar.uniqueName},
				
                { "remainingScraps", MoneyController.s.scraps },
                
                { "enemiesLeftAlive", EnemyHealth.enemySpawned - EnemyHealth.enemyKilled},
            }
        );
        
        
        Debug.Log("Mission Lost Analytics: " + analyticsResult);

        FMODMusicPlayer.s.PauseMusic();
        DirectControlMaster.s.DisableDirectControl();

        if(SettingsController.GamepadMode())
            EventSystem.current.SetSelectedGameObject(loseContinueButton);
    }


    /*public void Restart() {
        throw new NotImplementedException();
    }
    */


    public void BackToMenu() {
        isMissionLost = false;
        loseUI.SetActive(false);
        //MissionWinFinisher.s.ContinueToClearOutOfCombat();
        DataSaver.s.GetCurrentSave().currentRun = null;
        DataSaver.s.GetCurrentSave().isInARun = false;
        DataSaver.s.SaveActiveGame();

        // MusicPlayer.s.SwapMusicTracksAndPlay(false);
        //FMODMusicPlayer.s.SwapMusicTracksAndPlay(false);

        SceneLoader.s.ForceReloadScene();
    }
}
