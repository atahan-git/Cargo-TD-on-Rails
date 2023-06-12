using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FirstTimeTutorialController : MonoBehaviour {
    public static FirstTimeTutorialController s;

    private DataSaver.TutorialProgress _progress => DataSaver.s.GetCurrentSave().tutorialProgress;
    
    public bool initialCutsceneEngaged = false;
    public GameObject tutorialUI;

    [Space]
    public GameObject cameraHint; 
    
    public bool cameraWASDMoved = false;
    public bool cameraZoomed = false;
    public bool cameraRRotated = false;

    public List<GameObject> activeHints = new List<GameObject>();

    private void Awake() {
        s = this;
        tutorialUI.SetActive(false);
    }

    public void TutorialCheck() {
        tutorialUI.SetActive(false);
        ClearActiveHints();
        
        if (!MiniGUI_DisableTutorial.IsTutorialActive())
            return;

        if (!_progress.initialCutscenePlayed && !initialCutsceneEngaged) {
            EngageInitialCutscene();
        }else if (!_progress.firstCityTutorialDone) {
            tutorialUI.SetActive(true);
            
            leaveTheCity.SetActive(true);
            
            hoverOverThingsToGetInfo.SetActive(true);
            
            ShowCameraControls();
        }
    }

    public void NewCharacterCutsceneReset() {
        _progress.initialCutscenePlayed = !MiniGUI_DisableTutorial.IsTutorialActive();
    }

    public void ReDoTutorial() {
        //TutorialComplete();
        DataSaver.s.GetCurrentSave().isInARun = false;
        DataSaver.s.GetCurrentSave().tutorialProgress = new DataSaver.TutorialProgress();
        MiniGUI_DisableTutorial.SetVal(true);
        //ShopStateController.s.BackToMainMenu();
        SceneLoader.s.ForceReloadScene();
    }

    void EngageInitialCutscene() {
        initialCutsceneEngaged = true;

        cameraHint.SetActive(false);

        PlayStateMaster.s.OnCharacterSelected.AddListener(PlayObjectiveAnimation);
        if (DataSaver.s.GetCurrentSave().isInARun) {
            PlayObjectiveAnimation();
        }
    }

    void PlayObjectiveAnimation() {
        tutorialUI.SetActive(true);
        StartCoroutine(_PlayObjectiveAnimation());
    }

    public InputActionReference skip;

    public GameObject thisIsYourCargo;

    public GameObject leaveTheCity;
    public GameObject getRidOfEmpty;
    private bool emptyCartThingActive = false;
    private Cart emptyCart;

    public GameObject hoverOverThingsToGetInfo;

    public AnimationCurve worldMapLerp;
    public float worldMapLerpSpeed = 1f;
    
    IEnumerator _PlayObjectiveAnimation() {

        Cart cargo = null;
        emptyCart = null;

        for (int i = 0; i < Train.s.carts.Count; i++) {
            if (Train.s.carts[i].isCriticalComponent) {
                cargo = Train.s.carts[i];
            }

            if (Train.s.carts[i].modulesParent.childCount == 0) {
                emptyCart = Train.s.carts[i];
            }
        }
        
        
        
        thisIsYourCargo.SetActive(true);
        thisIsYourCargo.GetComponent<UIElementFollowWorldTarget>().SetUp(cargo.uiTargetTransform);

        ShopStateController.s.mapOpenButton.interactable = false;
        ShopStateController.s.starterUI.SetActive(false);
        
        CameraController.s.SetCameraControllerStatus(false);
        CameraController.s.SetMainCamPos();

        PlayerWorldInteractionController.s.canSelect = false;
        
        skip.action.Enable();

        GamepadControlsHelper.s.AddPossibleActions(GamepadControlsHelper.PossibleActions.cutsceneSkip);
        while (!skip.action.WasPerformedThisFrame()) {
            yield return null;
        }
        
        thisIsYourCargo.SetActive(false);
        
        WorldMapCreator.s.OpenWorldMap();
        CameraController.s.SetMainCamPos();
        WorldMapCreator.s.canSelectCastles = false;
        
        //WorldMapCreator.s.
        
        
        yield return WaitForSecondsSmart(2f);

        var lerpTarget = WorldMapCreator.s.yourObjectiveUIMarker.sourceTransform;
        var camLerpTransform = CameraController.s.cameraCenter;

        var curTimer = 0f;
        var startPos = camLerpTransform.position;

        while (curTimer <= 1f) {
            camLerpTransform.position = Vector3.Lerp(startPos, lerpTarget.position, worldMapLerp.Evaluate(curTimer));
            CameraController.s.SetMainCamPos();
            curTimer += Time.deltaTime *worldMapLerpSpeed;
            if(skip.action.WasPerformedThisFrame())
                break;
            yield return null;
        }
        camLerpTransform.position = lerpTarget.position;
        CameraController.s.SetMainCamPos();

        yield return WaitForSecondsSmart(2f);
        
        camLerpTransform.position = startPos;
        WorldMapCreator.s.ReturnToRegularMap();
        PlayerWorldInteractionController.s.canSelect = false;
        CameraController.s.SetMainCamPos();
        WorldMapCreator.s.canSelectCastles = true;
        
        if (!_progress.firstCityTutorialDone) {
            yield return WaitForSecondsSmart(1f);

            leaveTheCity.SetActive(true);
            CameraController.s.SetMainCamPos();

            yield return WaitForSecondsSmart(1f);

            if (emptyCart != null) {
                getRidOfEmpty.SetActive(true);
                getRidOfEmpty.GetComponent<UIElementFollowWorldTarget>().SetUp(emptyCart.uiTargetTransform);
                emptyCartThingActive = true;
            }

            CameraController.s.SetMainCamPos();

            yield return WaitForSecondsSmart(1f);
            
            hoverOverThingsToGetInfo.SetActive(true);
        }
        

        CameraController.s.SetCameraControllerStatus(true);
        PlayerWorldInteractionController.s.canSelect = true;
        ShopStateController.s.starterUI.SetActive(true);
        
        //yield return WaitForSecondsSmart(1f);
        
        ShopStateController.s.mapOpenButton.interactable = true;
        ShowCameraControls();
        
        GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.cutsceneSkip);
        
        InitialCutsceneComplete();
    }

    IEnumerator WaitForSecondsSmart(float toWait) {
        yield return null;
        var curTimer = 0f;
        while (curTimer <= toWait) {
            curTimer += Time.deltaTime;
            if(skip.action.WasPerformedThisFrame())
                break;
            yield return null;
        }
        yield return null;
    }


    void ShowCameraControls() {
        if (!_progress.cameraDone) {
            CameraController.s.rotateAction.action.performed += CameraRRotated;
            cameraHint.SetActive(true);
        } else {
            cameraHint.SetActive(false);
        }
    }

    void CameraRRotated(InputAction.CallbackContext obj) {
        cameraRRotated = true;
        CameraController.s.rotateAction.action.performed -= CameraRRotated;
    }

    private void Update() {
        if (!_progress.firstCityTutorialDone) {
            if (emptyCartThingActive) {
                var isOnTrain = emptyCart.myLocation == UpgradesController.CartLocation.train;
                var isLookingAtShop = !WorldMapCreator.s.worldMapOpen;
                getRidOfEmpty.SetActive(isOnTrain && isLookingAtShop);
            }
        }
        
        if (!_progress.cameraDone) {
            if (CameraController.s.moveAction.action.ReadValue<Vector2>().magnitude > 0||
                CameraController.s.moveGamepadAction.action.ReadValue<Vector2>().magnitude > 0 ) {
                cameraWASDMoved = true;
            }
            if (Mathf.Abs(CameraController.s.zoomAction.action.ReadValue<float>()) > 0 ||
                Mathf.Abs(CameraController.s.zoomGamepadAction.action.ReadValue<float>()) > 0) {
                cameraZoomed = true;
            }

            if (cameraWASDMoved && cameraZoomed && cameraRRotated) {
                _progress.cameraDone = true;
                Invoke(nameof(DisableCameraMovesetHint), 2f);
            }
        }
    }


    public GameObject repairHintPrefab;
    public GameObject repairCriticalHintPrefab;
    public GameObject reloadHintPrefab;
    public GameObject directControlHint;
    public void OnEnterCombat() {
        _progress.firstCityTutorialDone = true;
        ClearActiveHints();
        
        emptyCartThingActive = false;
        getRidOfEmpty.SetActive(false);
        leaveTheCity.SetActive(false);
        hoverOverThingsToGetInfo.SetActive(false);

        if (!MiniGUI_DisableTutorial.IsTutorialActive())
            return;


        for (int i = 0; i < Train.s.carts.Count; i++) {
            var cart = Train.s.carts[i];
            if (!_progress.directControlHint && cart.GetComponentInChildren<DirectControllable>()) {
                directControlHint.SetActive(true);
                _progress.directControlHint = true;
            }

            if (!_progress.reloadHint && cart.GetComponentInChildren<ModuleAmmo>()) {
                activeHints.Add(Instantiate(reloadHintPrefab, LevelReferences.s.uiDisplayParent).GetComponent<MiniGUI_TutorialHint>().SetUp(cart));
            }

            if (cart.isMainEngine || cart.isCriticalComponent) {
                if(!_progress.repairHint)
                    activeHints.Add(Instantiate(repairCriticalHintPrefab, LevelReferences.s.uiDisplayParent).GetComponent<MiniGUI_TutorialHint>().SetUp(cart));
            } else {
                if(!_progress.repairCriticalHint)
                    activeHints.Add(Instantiate(repairHintPrefab, LevelReferences.s.uiDisplayParent).GetComponent<MiniGUI_TutorialHint>().SetUp(cart));
            }
        }
    }

    void ClearActiveHints() {
        thisIsYourCargo.SetActive(false);
        for (int i = 0; i < activeHints.Count; i++) {
            if(activeHints[i] != null)
                Destroy(activeHints[i].gameObject);
        }
        
        activeHints.Clear();
    }


    public GameObject deliverCargoHintPrefab;
    public void OnFinishCombat() {
        ClearActiveHints();
        
        if (!MiniGUI_DisableTutorial.IsTutorialActive())
            return;
        
        for (int i = 0; i < Train.s.carts.Count; i++) {
            var cart = Train.s.carts[i];
            if (!_progress.deliverCargoHint && cart.GetComponentInChildren<CargoModule>()) {
                activeHints.Add(Instantiate(deliverCargoHintPrefab, LevelReferences.s.uiDisplayParent).GetComponent<MiniGUI_TutorialHint>().SetUp(cart));
            }
        }
    }

    public void OnEnterShop() {
        ClearActiveHints();
        TutorialCheck();
    }

    public void ReloadHintShown() {
        _progress.reloadHint = true;
    }

    public void RepairCriticalHintShown() {
        _progress.repairCriticalHint = true;
    }

    public void RepairHintShown() {
        _progress.repairHint = true;
    }

    public void CargoHintShown() {
        _progress.deliverCargoHint = true;
    }
    
    void DisableCameraMovesetHint() {
        _progress.cameraDone = true;
        cameraHint.SetActive(false);
    }

    void InitialCutsceneComplete() {
        initialCutsceneEngaged = false;
        DataSaver.s.GetCurrentSave().tutorialProgress.initialCutscenePlayed = true;
        DataSaver.s.SaveActiveGame();
    }
    

    public void RemoveAllTutorialStuff() {
        tutorialUI.SetActive(false);
        initialCutsceneEngaged = false;
        ClearActiveHints();
    }
}
