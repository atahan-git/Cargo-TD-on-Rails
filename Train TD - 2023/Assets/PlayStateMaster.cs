using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayStateMaster : MonoBehaviour {
    public static PlayStateMaster s;

    private void Awake() {
        s = this;
    }
    

    [NonSerialized]
    public UnityEvent OnMainMenuEntered = new UnityEvent();
    [NonSerialized]
    public UnityEvent OnOpenCharacterSelectMenu = new UnityEvent();
    [NonSerialized]
    public UnityEvent OnCharacterSelected = new UnityEvent();
    [NonSerialized]
    public UnityEvent OnDrawWorld = new UnityEvent();
    [NonSerialized]
    public UnityEvent OnNewWorldCreation = new UnityEvent();
    [NonSerialized]
    public UnityEvent OnShopEntered = new UnityEvent();
    [NonSerialized]
    public UnityEvent OnCombatEntered = new UnityEvent();
    
    [NonSerialized]
    public UnityEvent<bool> OnCombatFinished = new UnityEvent<bool>();
    [NonSerialized]
    public UnityEvent OnEnterMissionRewardArea = new UnityEvent();
    [NonSerialized]
    public UnityEvent OnLeavingMissionRewardArea = new UnityEvent();
    
    
    void SetDefaultCallbacks() {
        OnMainMenuEntered.AddListener(FirstTimeTutorialController.s.RemoveAllTutorialStuff);
        
        OnOpenCharacterSelectMenu.AddListener(CharacterSelector.s.CheckAndShowCharSelectionScreen);
        OnOpenCharacterSelectMenu.AddListener(MainMenu.s.ExitMainMenu);
        
        OnCharacterSelected.AddListener(Train.s.DrawTrainBasedOnSaveData);
        OnCharacterSelected.AddListener(UpgradesController.s.SetUpNewCharacterRarityBoosts);
        OnCharacterSelected.AddListener(FirstTimeTutorialController.s.NewCharacterCutsceneReset);
        OnCharacterSelected.AddListener(MoneyUIDisplay.totalMoney.OnCharLoad);
        
        OnDrawWorld.AddListener(WorldMapCreator.s.GenerateWorldMap);
        OnDrawWorld.AddListener(HexGrid.s.RefreshGrid);
        
        OnNewWorldCreation.AddListener(MapController.s.GenerateStarMap);
        OnNewWorldCreation.AddListener(OnDrawWorld.Invoke);

        OnShopEntered.AddListener(SpeedController.s.ResetDistance);
        OnShopEntered.AddListener(PlayStateMaster.s.ClearLevel);
        OnShopEntered.AddListener(Train.s.DrawTrainBasedOnSaveData);
        OnShopEntered.AddListener(WorldMapCreator.s.ReturnToRegularMap);
        OnShopEntered.AddListener(HexGrid.s.RefreshGrid);
        OnShopEntered.AddListener(ShopStateController.s.OpenShopUI);
        OnShopEntered.AddListener(FMODMusicPlayer.s.PlayMenuMusic);
        OnShopEntered.AddListener(MainMenu.s.ExitMainMenu);
        OnShopEntered.AddListener(Pauser.s.Unpause);
        OnShopEntered.AddListener(CharacterSelector.s.CheckAndShowCharSelectionScreen);
        OnShopEntered.AddListener(PlayerWorldInteractionController.s.OnEnterShopScreen);
        OnShopEntered.AddListener(FirstTimeTutorialController.s.OnEnterShop);
        OnShopEntered.AddListener(Train.s.OnEnterShopArea);
        OnShopEntered.AddListener(WorldDifficultyController.s.OnShopEntered);
        OnShopEntered.AddListener(ArtifactsController.s.OnEnterShop);
        OnShopEntered.AddListener(UpgradesController.s.OnShopOpened);
        OnShopEntered.AddListener(VignetteController.s.ResetVignette);
        
        OnCombatEntered.AddListener(FMODMusicPlayer.s.PlayCombatMusic);
        OnCombatEntered.AddListener(SpeedController.s.SetUpOnMissionStart);
        OnCombatEntered.AddListener(PathSelectorController.s.SetUpPath);
        OnCombatEntered.AddListener(FirstTimeTutorialController.s.OnEnterCombat);
        OnCombatEntered.AddListener(TimeController.s.OnCombatStart);
        OnCombatEntered.AddListener(PlayerWorldInteractionController.s.OnEnterCombat);
        OnCombatEntered.AddListener(UpgradesController.s.OnCombatStart);
        
        OnCombatFinished.AddListener(TimeController.s.OnCombatEnd);
        OnCombatFinished.AddListener(FirstTimeTutorialController.s.OnFinishCombat);
        OnCombatFinished.AddListener(EncounterController.s.ResetEncounter);
        OnCombatFinished.AddListener(SpeedController.s.OnCombatFinished);
        OnCombatFinished.AddListener(ArtifactsController.s.OnAfterCombat);
        OnCombatFinished.AddListener(Train.s.OnLeaveCombat);
        OnCombatFinished.AddListener(PlayerWorldInteractionController.s.OnLeaveCombat);
        
        OnEnterMissionRewardArea.AddListener(VignetteController.s.ResetVignette);
        OnEnterMissionRewardArea.AddListener(FirstTimeTutorialController.s.OnEnterShop);
        
        OnLeavingMissionRewardArea.AddListener(MissionWinFinisher.s.CleanupWhenLeavingMissionRewardArea);
        OnLeavingMissionRewardArea.AddListener(MapController.s.Cleanup);
        OnLeavingMissionRewardArea.AddListener(EnemyWavesController.s.Cleanup);
        OnLeavingMissionRewardArea.AddListener(ShopStateController.s.FinishTravellingToStar);
        OnLeavingMissionRewardArea.AddListener(ActFinishController.s.CloseActUI);
    }
    
    [SerializeField]
    private ConstructedLevel _currentLevel;

    public ConstructedLevel currentLevel {
        get {
            return _currentLevel;
        }
    }

    public enum GameState {
        mainMenu, shop, combat, levelFinished
    }

    [SerializeField] private GameState _gameState;

    
    public GameState myGameState {
        get {
            return _gameState;
        }
    }

    public bool isCombatStarted() {
        return myGameState == GameState.combat || myGameState == GameState.levelFinished;
    }
    public bool isCombatFinished() {
        return myGameState == GameState.levelFinished;
    }
    
    public bool isShop() {
        return myGameState == GameState.shop;
    }
    
    public bool isShopOrEndGame() {
        return myGameState == GameState.shop || myGameState == GameState.levelFinished;
    }
    
    public bool isEndGame() {
        return myGameState == GameState.levelFinished;
    }
    
    public bool isMainMenu() {
        return myGameState == GameState.mainMenu;
    }

    public void StarCombat() {
        _gameState = GameState.combat;
        OnCombatEntered?.Invoke();
    }

    public void FinishCombat(bool realCombat) {
        _gameState = GameState.levelFinished;
        OnCombatFinished?.Invoke(realCombat);
    }

    public bool isCombatInProgress() {
        return myGameState == GameState.combat;
    }
    
    public bool IsLevelSelected() {
        return _currentLevel != null;
    }

    public void SetCurrentLevel(ConstructedLevel levelData) {
        _currentLevel = levelData.Copy();

        if (MiniGUI_DebugLevelName.s != null) {
            MiniGUI_DebugLevelName.s.SetLevelName(_currentLevel.levelName);
        }
    }

    public void ClearLevel() {
        _currentLevel = null;
    }
    
    public void OpenMainMenu() {
        StopAllCoroutines();
        
        SceneLoader.s.ForceReloadScene();
        
        //StartCoroutine(Transition(false, () => DoOpenMainMenu()));
    }

    private void Start() {
        _gameState = GameState.mainMenu;
        OnMainMenuEntered?.Invoke();
        SetDefaultCallbacks();
    }

    

    /*void DoOpenMainMenu() {
        _gameState = GameState.mainMenu;
        MainMenu.s.OpenProfileMenu();

        if (isCombatInProgress()) {
            OnCombatFinished?.Invoke();
        }

        if (isCombatStarted()) {
            OnLeavingMissionRewardArea?.Invoke();
        }
        
        OnMainMenuEntered?.Invoke();
    }*/

    public void EnterMissionRewardArea() {
        OnEnterMissionRewardArea?.Invoke();
    }

    public void LeaveMissionRewardArea() {
        _gameState = GameState.shop;

        Train.s.RightBeforeLeaveMissionRewardArea();
        
        StopAllCoroutines();
        StartCoroutine(Transition(false, () => {
            OnLeavingMissionRewardArea?.Invoke();
            OnShopEntered?.Invoke();
        }));
    }

    public void EnterShopState() {
        _gameState = GameState.shop;

        StopAllCoroutines();

        if (DataSaver.s.GetCurrentSave().isInARun) {
            if (WorldGenerationProgress() >= 1f) {
                StartCoroutine(Transition(false, () => OnShopEntered?.Invoke()));
            } else {
                StartCoroutine(Transition(true, () => {
                    OnDrawWorld?.Invoke();
                }, WorldGenerationProgress,
                    () => {OnShopEntered?.Invoke();}));
            }
        } else {
            StartCoroutine(Transition(false, () => {
                OnOpenCharacterSelectMenu?.Invoke();
            }));
        }
    }


    public void FinishCharacterSelection() {
        _gameState = GameState.shop;

        StopAllCoroutines();
        WorldMapCreator.s.ResetWorldMapGenerationProgress();
        StartCoroutine(Transition(true,
            () => {
                CharacterSelector.s.CharSelectionCompleteAndScreenGotDark();
                OnCharacterSelected?.Invoke();
                OnNewWorldCreation?.Invoke();
            }, WorldGenerationProgress,
            () => {
                OnShopEntered?.Invoke();
            }
        ));
    }

    public void EnterNewAct() {
        _gameState = GameState.shop;
        
        Train.s.RightBeforeLeaveMissionRewardArea();
        
        StopAllCoroutines();
        WorldMapCreator.s.ResetWorldMapGenerationProgress();
        StartCoroutine(Transition(true,
            () => {
                OnLeavingMissionRewardArea?.Invoke();
                OnNewWorldCreation?.Invoke();
            }, 
            WorldGenerationProgress,
            () => {
                OnShopEntered?.Invoke();
            }
            ));
    }

    float WorldGenerationProgress() {
        return WorldMapCreator.s.worldMapGenerationProgress;
    }

    delegate float LoadDelegate();
    

    public bool isLoading = false;
    public float loadingProgress;
    public Slider loadingSlider;
    public GameObject loadingScreen;
    public GameObject loadingText;
    public CanvasGroup canvasGroup;
    public float currentFadeValue;
    public float fadeTime = 0.2f;
    public bool supressFadeOut = false;
    IEnumerator Transition(bool showLoading, Action toCallInTheMiddle, LoadDelegate loadProgress = null, Action toCallAtTheEnd = null) {
        isLoading = true;
        loadingProgress = 0;
        loadingSlider.value = loadingProgress;
        if(!showLoading)
            loadingText.SetActive(false);
        loadingScreen.SetActive(true);
        yield return StartCoroutine(FadeLoadingScreen(currentFadeValue,1, fadeTime-0.01f));

        yield return null; // one frame pause
        
        toCallInTheMiddle();

        if (showLoading && loadProgress != null) {
            while (loadingProgress < 1f) {
                loadingProgress = loadProgress();
                loadingSlider.value = loadingProgress;
                yield return null;
            }
        }

        if(toCallAtTheEnd != null)
            toCallAtTheEnd();

        if (supressFadeOut) {
            supressFadeOut = false;
            yield break;
        }

        yield return StartCoroutine(FadeLoadingScreen(1,0, fadeTime));
        loadingScreen.SetActive(false);
        loadingText.SetActive(true);
        isLoading = false;
    }
    
    IEnumerator FadeLoadingScreen(float startValue, float targetValue, float duration)
    {
        float time = 0;

        while (time < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startValue, targetValue, time / duration);
            currentFadeValue = canvasGroup.alpha;
            time += Time.unscaledDeltaTime;
            yield return null;
        }
        canvasGroup.alpha = targetValue;
    }
}
