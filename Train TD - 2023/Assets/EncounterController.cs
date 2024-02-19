using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class EncounterController : MonoBehaviour {
    public static EncounterController s;

    public CameraController camCont;

    public GameObject encounterMasterUI;
    
    public GameObject encounterTextUI;
    public TMP_Text titleText;
    public TMP_Text mainText;
    public Image image;

    public Transform optionsParent;
    public GameObject optionsPrefab;
    public GameObject requirementOrRewardPrefab;

    public EncounterTitle currentEncounter;
    public EncounterNode currentNode;


    public InputActionReference stopByAction;
    public InputActionReference ridePastAction;

    
    private void Awake() {
        s = this;
    }

    private void Start() {
        ResetEncounter();
    }

    public void ResetEncounter(bool realCombat= true) {
        encounterMasterUI.SetActive(false);
        encounterTextUI.SetActive(false);
        fadeUI.SetActive(false);
        encounterStopByUI.gameObject.SetActive(false);
        encounterStopByUI.optionChoosen = true;
        
        youRodePast.SetActive(false);
        enemyAmbush.SetActive(false);

        if (encounterObj != null) {
            Destroy(encounterObj);
            encounterObj = null;
        }
        
        StopAllCoroutines();
        CancelInvoke();

        SpeedController.s.encounterOverride = false;
        EnemyWavesController.s.encounterMode = false;
        
        GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.encounterButtons);
    }

    private void OnEnable() {
        stopByAction.action.Enable();
        ridePastAction.action.Enable();
        stopByAction.action.performed += Stop;
        ridePastAction.action.performed += RidePast;
    }

    private void RidePast(InputAction.CallbackContext obj) {
        encounterStopByUI.RidePast();
    }

    private void Stop(InputAction.CallbackContext obj) {
        encounterStopByUI.Stop();
    }

    private void OnDisable() {
        stopByAction.action.Disable();
        ridePastAction.action.Disable();
        stopByAction.action.performed -= Stop;
        ridePastAction.action.performed -= RidePast;
    }


    /*public void EngageEncounter(LevelData data) {
        EngageEncounter(data.levelName);
    }*/

    public MiniGUI_EncounterStopBy encounterStopByUI;

    public GameObject encounterObj;
    public GameObject youRodePast;
    public GameObject enemyAmbush;
    public void EngageEncounter(string encounterName) {
        ResetEncounter();
        
        encounterObj = Instantiate(DataHolder.s.GetEncounter(encounterName), Vector3.forward*100, Quaternion.identity);
        currentEncounter = encounterObj.GetComponent<EncounterTitle>();
        currentNode = currentEncounter.initialNode;

        EnemyWavesController.s.encounterMode = true;

        doAmbush = false;
        encounterSkipped = false;
        
        Invoke(nameof(ReallyEngageEncounter), 3f);
    }

    public void ReallyEngageEncounter() {
        encounterMasterUI.SetActive(true);
        encounterStopByUI.gameObject.SetActive(true);
        encounterStopByUI.SetUp(currentEncounter);
        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.encounterButtons);
    }
    
    
    public void Stop() {
        if (currentEncounter != null) {
            
            encounterStopByUI.gameObject.SetActive(false);
            
            StartCoroutine(EncounterStartAnimation());
        }
    }

    public void RidePastBeforeEncounter() {
        if (currentEncounter != null) {
            // do stuff like secret ambush
            if (Random.value < currentEncounter.ambushChance) {
                doAmbush = true;
            } else {
                encounterSkipped = true;
            }
            
            encounterStopByUI.gameObject.SetActive(false);
            encounterTextUI.SetActive(false);

            StartCoroutine(RidePastAnimation());
        }
    }
    
    public void RidePastAfterEncounterOver() {
        if (currentEncounter != null) {
            // do stuff like secret ambush
            
            encounterStopByUI.gameObject.SetActive(false);
            encounterTextUI.SetActive(false);

            StartCoroutine(RidePastAnimation());
        }
    }

    public bool doAmbush = false;
    public bool encounterSkipped = false;

    IEnumerator RidePastAnimation() {
        fadeUI.SetActive(true);
        var ridePastFade = 1f;
        
        StartCoroutine(FadeOverlay(0, 1, ridePastFade));
        yield return new WaitForSeconds(ridePastFade );
        
        yield return new WaitForSeconds(0.5f);
        if (doAmbush) {
            //EnemyWavesController.s.SpawnAmbush(ridePastAmbush.GetData());
        }

        yield return new WaitForSeconds(0.5f);
        
        StartCoroutine(FadeOverlay(1, 0, ridePastFade));
        if (doAmbush) {
            enemyAmbush.SetActive(true);
        }else if (encounterSkipped) {
            youRodePast.SetActive(true);
        }
        Invoke(nameof(EncounterComplete), 2f);
    }

    public GameObject fadeUI;
    public Image fadeOverlay;
    public float fadeDurations = 0.5f;

    private float a;
    IEnumerator EncounterStartAnimation() {
        fadeUI.SetActive(true);
        StartCoroutine(FadeOverlay(0, 1, fadeDurations));
        
        yield return new WaitForSeconds(fadeDurations);
        
        encounterObj.transform.position = Vector3.zero;

        SpeedController.s.encounterOverride = true;
        LevelReferences.s.speed = 0;

        StartCoroutine(FadeOverlay(1, 0, fadeDurations));
        
        var train = Train.s.gameObject;
        camCont.enabled = false;

        var encounterCamPos = currentEncounter.encounterCam;
        MainCameraReference.s.cam.transform.position = encounterCamPos.position;
        MainCameraReference.s.cam.transform.rotation = encounterCamPos.rotation;

        var pos = train.transform.position;
        
        // s = (v+u)t/2
        // v = 2s/t
        float dist = 15;
        float timer = 3f;
        
        float v = 2*dist/timer;
        a = v/timer;


        pos.z = -dist + (train.transform.position - Train.s.trainFront.position).z;

        SpeedController.s.currentBreakPower = 10;
        while (timer > 0f) {
            pos.z += v * Time.deltaTime;
            v += -a * Time.deltaTime;
            train.transform.position = pos;

            timer -= Time.deltaTime;
            yield return null;
        }
        SpeedController.s.currentBreakPower = 0;
        
        yield return new WaitForSeconds(0.5f);

        StartCoroutine(FadeOverlay(0, 1, fadeDurations));
        yield return new WaitForSeconds(fadeDurations);
        
        train.transform.position = Vector3.zero;
        camCont.enabled = true;
        
        StartEncounter();
        encounterStopByUI.gameObject.SetActive(false);
        encounterTextUI.SetActive(true);
        
        
        StartCoroutine(FadeOverlay(1, 0, fadeDurations));
        fadeUI.SetActive(false);
    }
    
    IEnumerator FadeOverlay(float startValue, float targetValue, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            var col = fadeOverlay.color;
            col.a = Mathf.Lerp(startValue, targetValue, time / duration);
            fadeOverlay.color = col;
            
            time += Time.deltaTime;
            yield return null;
        }
        var col2 = fadeOverlay.color;
        col2.a = targetValue;
        fadeOverlay.color = col2;
    }

    void StartEncounter() {
        encounterTextUI.SetActive(true);

        image.sprite = currentEncounter.image;
        titleText.text = currentEncounter.title;
        
        foreach (var randomizer in currentEncounter.GetComponentsInChildren<EncounterRandomlyEnableOption>()) {
            randomizer.Randomize();
        }
        
        foreach (var reward in currentEncounter.GetComponentsInChildren<EncounterReward>()) {
            reward.RandomizeReward();
        }
        
        foreach (var requirement in currentEncounter.GetComponentsInChildren<EncounterRequirement>()) {
            requirement.RandomizeRequirement();
        }
        
        MoveToNewNode(currentEncounter.initialNode);
    }


    public void MoveToNewNode(EncounterNode node) {
        if (node == null) {
            encounterTextUI.SetActive(false);
            StartCoroutine(EncounterEndAnimation());
            return;
        }

        currentNode = node;
        mainText.text = currentNode.text;

        optionsParent.DeleteAllChildren();

        var currentOptions = currentNode.GetComponentsInChildren<EncounterOption>();
        for (int i = 0; i < currentOptions.Length; i++) {
            if (currentOptions[i].optionEnabled) {
                var option = Instantiate(optionsPrefab, optionsParent);
                option.GetComponent<MiniGUI_EncounterOption>().SetUp(currentOptions[i]);
            }
        }
    }

    void EncounterComplete() {
        ResetEncounter();
        
        PlayerWorldInteractionController.s.canSelect = true;
        
    }
    
    
    IEnumerator EncounterEndAnimation() {
        var train = Train.s.gameObject;
        camCont.enabled = false;

        var pos = train.transform.position;
        
        
        float timer = fadeDurations + PlayStateMaster.s.fadeTime;
        
        float v = 0;

        while (timer > 0f) {
            pos.z += v * Time.deltaTime;
            v += a * Time.deltaTime;
            train.transform.position = pos;

            timer -= Time.deltaTime;
            yield return null;
        }

        train.transform.position = Vector3.zero;
        camCont.enabled = true;
        
        EncounterComplete();
    }
}
