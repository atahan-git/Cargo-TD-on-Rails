using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
    private bool moveMainCam = false;
    private bool mainCamLerpComplete = false;
    public Transform mainCamMoveTarget;
    public RawImage blackBackgroundImage;
    public CanvasGroup fullBlackOverlay;
    public Camera deathCam;
    IEnumerator DeathAnimation() {
        isDeathAnimPlaying = true;
        mainCamLerpComplete = false;
        moveMainCam = true;
        
        var _camTrans = MainCameraReference.s.cam.transform;
        mainCamMoveTarget.position = _camTrans.position;
        mainCamMoveTarget.rotation = _camTrans.rotation;

        blackBackgroundImage.gameObject.SetActive(true);
        blackBackgroundImage.color = new Color(1, 1, 1, 0);

        TimeController.s.SetTimeSlowForDetailScreen(true);


        yield return new WaitForSecondsRealtime(2);

        deathCam.enabled = true;

        AsteroidInTheDistancePositionController.s.enabled = false;
        var meteor = AsteroidInTheDistancePositionController.s.transform;
        var meteorStartPos = meteor.position;
        var enginePos = Train.s.carts[0].transform.position;
        var direction = meteorStartPos - enginePos;
        //meteorStartPos = direction.normalized * 150;
        meteor.localScale = Vector3.one * 0.25f;

        var lerpCam = 0f;
        while (lerpCam <= 1f) {
            var color = Color.white;
            color.a = Mathf.Clamp01(lerpCam.Remap(0,1,0,0.75f));
            blackBackgroundImage.color = color;
            
            meteor.position = Vector3.Lerp(meteorStartPos, enginePos, lerpCam);
            
            lerpCam += Time.unscaledDeltaTime/2f;
            yield return null;
        }
        blackBackgroundImage.color = Color.white;
        
        meteor.gameObject.SetActive(false);
        fullBlackOverlay.gameObject.SetActive(true);
        
        
        deathCam.enabled = false;
        moveMainCam = false;
        CameraController.s.enabled = true;
        NewspaperController.s.OpenNewspaperScreen();
        blackBackgroundImage.gameObject.SetActive(false);
        
        yield return new WaitForSecondsRealtime(2);
        
        if (DataSaver.s.GetCurrentSave().instantRestart) {
            BackToMenu();
            yield break;
        } else {
            var lerpBlack = 0f;
            while (lerpBlack <= 1f) {
                fullBlackOverlay.alpha = Mathf.Clamp01(lerpBlack.Remap(0, 1, 1, 0));
                lerpBlack += Time.unscaledDeltaTime / 2f;
                yield return null;
            }

            fullBlackOverlay.gameObject.SetActive(false);

            PlayerWorldInteractionController.s.canSelect = true;

            isDeathAnimPlaying = false;
        }
    }

    private void LateUpdate() {
        if (isDeathAnimPlaying) {
            if (moveMainCam) {
                var _camTrans = MainCameraReference.s.cam.transform;

                deathCam.transform.position = _camTrans.position;
                deathCam.transform.rotation = _camTrans.rotation;

                if (!mainCamLerpComplete) {
                    var engine = Train.s.carts[0].transform;

                    var targetPosition = engine.position + engine.right * 2.73f + engine.up * 3.62f;
                    mainCamMoveTarget.position = Vector3.Lerp(mainCamMoveTarget.position, targetPosition, 0.2f * Time.unscaledDeltaTime);
                    mainCamMoveTarget.LookAt(engine);

                    _camTrans.position = mainCamMoveTarget.position;
                    _camTrans.rotation = Quaternion.Slerp(_camTrans.rotation, mainCamMoveTarget.rotation, 1f * Time.unscaledDeltaTime);

                    if (Vector3.Distance(mainCamMoveTarget.position, targetPosition) < 0.01f) {
                        mainCamLerpComplete = true;
                        mainCamMoveTarget.position = targetPosition;
                    }
                } else {
                    _camTrans.position = mainCamMoveTarget.position;
                    _camTrans.rotation = mainCamMoveTarget.rotation;
                }
            }
        }
    }

    public void Continue() {
        BackToMenu();
    }


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
