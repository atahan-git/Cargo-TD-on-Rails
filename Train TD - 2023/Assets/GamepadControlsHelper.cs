using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GamepadControlsHelper : MonoBehaviour {
    public static GamepadControlsHelper s;

    private void Awake() {
        s = this;
        cartSelectPrompts.gameObject.SetActive(false);
        gatePrompt.gameObject.SetActive(false);
    }
    
    public enum PossibleActions {
        move=0, reloadControl=1, repairControl=2, gunControl=3, openMap=4, pause=5, fastForward=6, showDetails=7, shoot=8, exitDirectControl=9, flipCamera=10, cutsceneSkip=11, clickGate=12, changeTrack=13, engineControl=14,
        encounterButtons=15, shieldControl=19, moveHoldGamepad=20, repairDroneMove=21, repairDroneUp=22, repairDroneDown=23, engineControlSwitch=24
    }

    public GameObject gamepadSelector;
    public UIElementFollowWorldTarget cartSelectPrompts;
    
    public UIElementFollowWorldTarget gatePrompt;

    public List<ButtonPrompt> buttonPrompts = new List<ButtonPrompt>();

    public List<PossibleActions> currentlyLegalActions = new List<PossibleActions>();

    private void Start() {
        AddActionsAlwaysAvailable();
        PlayerWorldInteractionController.s.OnSelectSomething.AddListener(UpdateButtonPromptsLocation);
        PlayerWorldInteractionController.s.OnSelectGate.AddListener(UpdateGateSelectPrompt);
    }

    private void UpdateButtonPromptsLocation(IPlayerHoldable target, bool isSelecting) {
        if (isSelecting) {
            cartSelectPrompts.SetUp(target.GetUITargetTransform());
            cartSelectPrompts.gameObject.SetActive(true);
        } else {
            gatePrompt.gameObject.SetActive(false);
            cartSelectPrompts.gameObject.SetActive(false);
        }
    }
    
    void UpdateGateSelectPrompt(IClickableWorldItem gateScript, bool isSelecting) {
        if (isSelecting) {
            gatePrompt.SetUp((gateScript as MonoBehaviour).transform);
            gatePrompt.gameObject.SetActive(true);
        } else {
            gatePrompt.gameObject.SetActive(false);
        }
    }

    void AddActionsAlwaysAvailable() {
        currentlyLegalActions.Add(PossibleActions.pause);
        currentlyLegalActions.Add(PossibleActions.flipCamera);
        currentlyLegalActions.Add(PossibleActions.changeTrack);
        UpdateButtonPrompts();
    }

    public void AddPossibleActions(PossibleActions toAdd) {
        if (!currentlyLegalActions.Contains(toAdd)) {
            currentlyLegalActions.Add(toAdd);
            UpdateButtonPrompts();
        }
    }

    public void RemovePossibleAction(PossibleActions toRemove) {
        if (currentlyLegalActions.Contains(toRemove)) {
            currentlyLegalActions.Remove(toRemove);
            UpdateButtonPrompts();
        }
    }

    public void UpdateButtonPrompts() {
        var gamepadMode = SettingsController.GamepadMode();
        if (SettingsController.ShowButtonPrompts()) {
            for (int i = 0; i < buttonPrompts.Count; i++) {
                buttonPrompts[i].SetState(currentlyLegalActions.Contains(buttonPrompts[i].myAction), gamepadMode);
            }
        } else {
            for (int i = 0; i < buttonPrompts.Count; i++) {
                buttonPrompts[i].SetState(false, gamepadMode);
            }
        }
    }

    public float rotateSpeed = 20f;
    public float regularSize = 1f;
    public float clickSize = 0.7f;
    public float sizeLerpSpeed = 1f;

    public InputActionReference clickAction;

    // Update is called once per frame
    void Update()
    {
        if (SettingsController.GamepadMode() && PlayerWorldInteractionController.s.canSelect) {
            gamepadSelector.SetActive(true);


            if (clickAction.action.IsPressed()) {
                gamepadSelector.transform.localScale = Vector3.Lerp(gamepadSelector.transform.localScale, Vector3.one * clickSize, sizeLerpSpeed * Time.deltaTime);

            } else {
                gamepadSelector.transform.localScale = Vector3.Lerp(gamepadSelector.transform.localScale, Vector3.one * regularSize, sizeLerpSpeed * Time.deltaTime);
                gamepadSelector.transform.Rotate(0,rotateSpeed*Time.deltaTime,0);
                
            }

        } else {
            gamepadSelector.SetActive(false);
            //cartSelectPrompts.gameObject.SetActive(false);
        }
        
        
        if(cartSelectPrompts.sourceTransform == null)
            cartSelectPrompts.gameObject.SetActive(false);
    }

    public Ray GetRay() {
        return new Ray(gamepadSelector.transform.position + Vector3.up * 3, Vector3.down);
    }

    public Vector3 GetTooltipPosition() {
        return gamepadSelector.transform.position + Vector3.up * 0.5f;
    }
}
