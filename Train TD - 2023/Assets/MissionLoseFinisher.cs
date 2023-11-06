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
        noEngine, noMysteryCargo, abandon, everyCartExploded
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
            case MissionLoseReason.everyCartExploded:
                loseReasonText.text = "Your entire train is broken.";
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


        /*if (DataSaver.s.GetCurrentSave().currentRun.currentAct >= 2) { // only award recovered artifacts if player beats the first boss
            var allArtifacts = ArtifactsController.s.myArtifacts;

            var eligibleComponents = new List<Artifact>();
            var eligibleGems = new List<Artifact>();
            for (int i = 0; i < allArtifacts.Count; i++) {
                switch (allArtifacts[i].myRarity) {
                    case UpgradesController.CartRarity.common:
                    case UpgradesController.CartRarity.rare:
                    case UpgradesController.CartRarity.epic:
                        eligibleGems.Add(allArtifacts[i]);
                        break;
                    case UpgradesController.CartRarity.boss:
                        eligibleComponents.Add(allArtifacts[i]);
                        break;
                    case UpgradesController.CartRarity.special:
                        // do nothing. We never recover special artifacts
                        break;
                }
            }

            var eligibleCarts = new List<Cart>();

            for (int i = 0; i < Train.s.carts.Count; i++) {
                var cart = Train.s.carts[i];
                if (!cart.isCargo && !cart.isMainEngine && !cart.isMysteriousCart && cart.myRarity != UpgradesController.CartRarity.special) {
                    eligibleCarts.Add(cart);
                }
            }

            if (eligibleComponents.Count > 0) {
                DataSaver.s.GetCurrentSave().metaProgress.bonusComponent = eligibleComponents[Random.Range(0, eligibleComponents.Count)].uniqueName;
            }

            if (eligibleGems.Count > 0) {
                DataSaver.s.GetCurrentSave().metaProgress.bonusGem = eligibleGems[Random.Range(0, eligibleGems.Count)].uniqueName;
            }

            if (eligibleCarts.Count > 0) {
                DataSaver.s.GetCurrentSave().metaProgress.bonusCart = eligibleCarts[Random.Range(0, eligibleCarts.Count)].uniqueName;
            }
        }*/

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
