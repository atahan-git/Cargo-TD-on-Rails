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
        //OnMainMenuEntered.AddListener(FirstTimeTutorialController.s.RemoveAllTutorialStuff);
        
        /*OnCharacterSelected.AddListener(Train.s.DrawTrainBasedOnSaveData);
        OnCharacterSelected.AddListener(UpgradesController.s.SetUpNewCharacterRarityBoosts);
        OnCharacterSelected.AddListener(FirstTimeTutorialController.s.NewCharacterCutsceneReset);
        OnCharacterSelected.AddListener(MoneyUIDisplay.totalMoney.OnCharLoad);*/
        
        OnDrawWorld.AddListener(PathAndTerrainGenerator.s.SetBiomes);
        OnDrawWorld.AddListener(PathAndTerrainGenerator.s.MakeStarterAreaTerrain);
        
        OnNewWorldCreation.AddListener(OnDrawWorld.Invoke);

        // if adding anything here make sure to add it to Prologue controller too
        OnShopEntered.AddListener(PlayStateMaster.s.ClearLevel);
        OnShopEntered.AddListener(Train.s.DrawTrainBasedOnSaveData);
        OnShopEntered.AddListener(ShopStateController.s.OpenShopUI);
        OnShopEntered.AddListener(FMODMusicPlayer.s.PlayMenuMusic);
        OnShopEntered.AddListener(MainMenu.s.ExitMainMenu);
        OnShopEntered.AddListener(Pauser.s.Unpause);
        OnShopEntered.AddListener(PlayerWorldInteractionController.s.OnEnterShopScreen);
        OnShopEntered.AddListener(FirstTimeTutorialController.s.OnEnterShop);
        OnShopEntered.AddListener(Train.s.OnEnterShopArea);
        OnShopEntered.AddListener(WorldDifficultyController.s.OnShopEntered);
        OnShopEntered.AddListener(VignetteController.s.ResetVignette);
        OnShopEntered.AddListener(ScreenFadeToWhiteController.s.ResetFadeToWhite);
        OnShopEntered.AddListener(LevelReferences.s.ClearCombatHoldableThings);
        OnShopEntered.AddListener(NewspaperController.s.CheckShowNewspaper);
        OnShopEntered.AddListener(Act2DemoEndController.s.OnEnterShop);
        OnShopEntered.AddListener(ResetShopBuildings.s.OnShopEntered);
        
        OnCombatEntered.AddListener(WorldDifficultyController.s.OnCombatStart);
        OnCombatEntered.AddListener(FMODMusicPlayer.s.PlayCombatMusic);
        OnCombatEntered.AddListener(SpeedController.s.SetUpOnMissionStart);
        OnCombatEntered.AddListener(PathAndTerrainGenerator.s.MakeLevelTerrain);
        OnCombatEntered.AddListener(PathSelectorController.s.SetUpPath);
        OnCombatEntered.AddListener(FirstTimeTutorialController.s.OnEnterCombat);
        OnCombatEntered.AddListener(TimeController.s.OnCombatStart);
        OnCombatEntered.AddListener(PlayerWorldInteractionController.s.OnEnterCombat);
        OnCombatEntered.AddListener(ShopStateController.s.OnCombatStart);
        OnCombatEntered.AddListener(CrystalsAndWarpController.s.OnCombatStart);
        OnShopEntered.AddListener(Act2DemoEndController.s.OnStartCombat);
        
        OnCombatFinished.AddListener(WorldDifficultyController.s.OnCombatEnd);
        OnCombatFinished.AddListener(TimeController.s.OnCombatEnd);
        OnCombatFinished.AddListener(FirstTimeTutorialController.s.OnFinishCombat);
        OnCombatFinished.AddListener(EncounterController.s.ResetEncounter);
        OnCombatFinished.AddListener(PlayerWorldInteractionController.s.OnLeaveCombat);
        
        /*OnEnterMissionRewardArea.AddListener(VignetteController.s.ResetVignette);
        OnEnterMissionRewardArea.AddListener(FirstTimeTutorialController.s.OnEnterShop);*/
        
        /*OnLeavingMissionRewardArea.AddListener(MissionWinFinisher.s.CleanupWhenLeavingMissionRewardArea);
        OnLeavingMissionRewardArea.AddListener(EnemyWavesController.s.Cleanup);*/
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
        SetDefaultCallbacks();
        OnDrawWorld?.Invoke();

        if (enterShopOnLoad) {
            enterShopOnLoad = false;
            MainMenu.s.OpenProfileMenu(); // we need this to disable a couple of things
            MainMenu.s.StartGame();
        } else {
            FirstTimeTutorialController.s.RemoveAllTutorialStuff();
            _gameState = GameState.mainMenu;
            MainMenu.s.OpenProfileMenu();
            if (!DataSaver.s.GetCurrentSave().tutorialProgress.prologueDone) {
                Debug.Log("Would've started prologue automatically but that's disabled for now.");
                //PrologueController.s.EngagePrologue();
            }
        }
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

    public static bool enterShopOnLoad = false;
    public void LeaveMissionRewardAreaAndEnterShopState() {
        Train.s.RightBeforeLeaveMissionRewardArea();
        enterShopOnLoad = true;
        
        OnLeavingMissionRewardArea?.Invoke();
        
        StartCoroutine(Transition(false, () => { SceneLoader.s.ForceReloadScene(); }, NoProgress));

        /*_gameState = GameState.shop;

        Train.s.RightBeforeLeaveMissionRewardArea();
        
        StopAllCoroutines();
        /*StartCoroutine(Transition(false, () => {
            OnLeavingMissionRewardArea?.Invoke();
            OnShopEntered?.Invoke();
        }));#1#
        
        StartCoroutine(Transition(true, () => {
                OnLeavingMissionRewardArea?.Invoke();
                OnDrawWorld?.Invoke();
            }, WorldGenerationProgress,
            () => {OnShopEntered?.Invoke();}));*/
    }

    public void FinishWarpToTeleportBackToShop() {
        Train.s.ShowEntrySparkles();
        enterShopOnLoad = true;
        
        StartCoroutine(Transition(false, () => { SceneLoader.s.ForceReloadScene(); }, NoProgress));
    }
    
    public void EnterShopState() {
        _gameState = GameState.shop;

        StopAllCoroutines();

        StartCoroutine(Transition(true, () => {
                OnDrawWorld?.Invoke();
            }, WorldGenerationProgress,
            () => {OnShopEntered?.Invoke();}));
    }

    public void EnterPrologue() {
        _gameState = GameState.combat;
        
        StopAllCoroutines();
        
        StartCoroutine(Transition(true, () => {
                PrologueController.s.DrawPrologueWorld();
            }, WorldGenerationProgress,
            () => {PrologueController.s.PrologueLoadComplete();}));
    }

    public void EnterNewAct() {
        _gameState = GameState.shop;
        
        Train.s.RightBeforeLeaveMissionRewardArea();
        
        StopAllCoroutines();
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

    float NoProgress() {
        return 0f;
    }
    
    float WorldGenerationProgress() {
        return PathAndTerrainGenerator.s.terrainGenerationProgress;
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

        if (loadProgress != null) {
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
        yield return null;
        float time = 0;

        while (time < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startValue, targetValue, time / duration);
            currentFadeValue = canvasGroup.alpha;
            time += Time.unscaledDeltaTime;
            yield return null;
        }
        canvasGroup.alpha = targetValue;
        yield return null;
    }
}
