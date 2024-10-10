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

    public enum MissionLoseReason {
        noEngine, noMysteryCargo, abandon, everyCartExploded, endOfDemo
    }

    public void MissionLost(MissionLoseReason loseReason) {
        if (isMissionLost)
            return;

        DataSaver.s.GetCurrentSave().tutorialProgress.prologueDone = true;
        DataSaver.s.GetCurrentSave().showWakeUp = true;
        DataSaver.s.GetCurrentSave().isInARun = false;
        if (PrologueController.s.isPrologueActive) {
            PrologueController.s.PrologueDone(); 
        } else {
            DataSaver.s.GetCurrentSave().tutorialProgress.runsMadeAfterTutorial += 1;
            DataSaver.s.GetCurrentSave().runsMade += 1;
        }

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
            case MissionLoseReason.endOfDemo:
                loseReasonText.text = $"You have reached the end of current state of the game! Atahan needs to add more stuff before you can continue.";
                break;
            default:
                loseReasonText.text = "You cannot continue for unknown reasons (Atahan needs to fill this in!)";
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
        

        DataSaver.s.SaveActiveGame();

        /*AnalyticsResult analyticsResult = Analytics.CustomEvent(
            "LevelLost",
            new Dictionary<string, object> {
                { "Level", PlayStateMaster.s.currentLevel.levelName },
                { "distance", Mathf.RoundToInt(SpeedController.s.currentDistance / 10) *10},
                { "time", Mathf.RoundToInt(WorldDifficultyController.s.GetMissionTime()/10) * 10},

            }
        );*/
        
        
        //Debug.Log("Mission Lost Analytics: " + analyticsResult);

        FMODMusicPlayer.s.PauseMusic();
        DirectControlMaster.s.DisableDirectControl();


        StartCoroutine(DeathAnimation());
    }

    private bool isDeathAnimPlaying = false;
    private bool mainCamLerpComplete = false;
    public Transform mainCamMoveTarget;
    IEnumerator DeathAnimation() {
        isDeathAnimPlaying = true;
        mainCamLerpComplete = false;

        var engine = Train.s.carts[0].transform;

        mainCamMoveTarget.position = engine.position + engine.right * 2.73f + engine.up * 3.62f;
        mainCamMoveTarget.LookAt(engine);
        
        
        yield return null;
        
        
        if (DataSaver.s.GetCurrentSave().instantRestart) {
            BackToMenu();
        }// else the player will need to click back to menu on their own

        //isDeathAnimPlaying = false;
    }

    private void LateUpdate() {
        if (isDeathAnimPlaying) {
            if (mainCamLerpComplete) {
                var _camTrans = MainCameraReference.s.cam.transform;
                if (!mainCamLerpComplete) {
                    if (
                        Vector3.Distance(_camTrans.transform.position, mainCamMoveTarget.position) > 0.01f
                        && Quaternion.Angle(_camTrans.transform.rotation, mainCamMoveTarget.rotation) > 1
                    ) {
                        _camTrans.transform.position = Vector3.Lerp(_camTrans.position, mainCamMoveTarget.position, 5*Time.deltaTime);
                        _camTrans.transform.rotation = Quaternion.Slerp(_camTrans.rotation, mainCamMoveTarget.rotation, 20*Time.deltaTime);
                    } else {
                        mainCamLerpComplete = true;
                    }
                } else {
                    _camTrans.position = mainCamMoveTarget.position;
                    _camTrans.rotation = mainCamMoveTarget.rotation;
                }
            }
        }
    }


    /*public void Restart() {
        throw new NotImplementedException();
    }
    */


    public void BackToMenu() {
        isMissionLost = false;
        loseUI.SetActive(false);
        //MissionWinFinisher.s.ContinueToClearOutOfCombat();
        DataSaver.s.SaveActiveGame();

        // MusicPlayer.s.SwapMusicTracksAndPlay(false);
        //FMODMusicPlayer.s.SwapMusicTracksAndPlay(false);

        SceneLoader.s.ForceReloadScene();
    }
}
