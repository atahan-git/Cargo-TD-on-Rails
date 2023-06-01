using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GamepadControlsHelper : MonoBehaviour {
    public static GamepadControlsHelper s;

    private void Awake() {
        s = this;
        cartSelectPrompts.gameObject.SetActive(false);
        gatePrompt.gameObject.SetActive(false);
    }
    
    public enum PossibleActions {
        move, reload, repair, directControl, openMap, pause, fastForward, showDetails, shoot, exitDirectControl, flipCamera, cutsceneSkip, clickGate, changeTrack
    }

    public GameObject gamepadSelector;
    public UIElementFollowWorldTarget cartSelectPrompts;
    
    public UIElementFollowWorldTarget gatePrompt;

    public List<ButtonPrompt> buttonPrompts = new List<ButtonPrompt>();

    public List<PossibleActions> currentlyLegalActions = new List<PossibleActions>();

    private void Start() {
        AddActionsAlwaysAvailable();
        PlayerWorldInteractionController.s.OnSelectBuilding.AddListener(UpdateCartButtonPromptsLocation);
        PlayerWorldInteractionController.s.OnSelectGate.AddListener(UpdateGateSelectPrompt);
    }

    private void UpdateCartButtonPromptsLocation(Cart cart, bool isSelecting) {
        if (isSelecting) {
            cartSelectPrompts.SetUp(cart.uiTargetTransform);
            cartSelectPrompts.gameObject.SetActive(true);
        } else {
            cartSelectPrompts.gameObject.SetActive(false);
        }
    }

    void UpdateGateSelectPrompt(GateScript gateScript, bool isSelecting) {
        if (isSelecting) {
            gatePrompt.SetUp(gateScript.transform);
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
    }

    public Ray GetRay() {
        return new Ray(gamepadSelector.transform.position + Vector3.up * 3, Vector3.down);
    }

    public Vector3 GetTooltipPosition() {
        return gamepadSelector.transform.position + Vector3.up * 0.5f;
    }
}
