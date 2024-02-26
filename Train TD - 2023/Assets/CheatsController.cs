using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CheatsController : MonoBehaviour
{
    public InputActionReference cheatButton;
    
    public EncounterTitle debugEncounter;
    public PowerUpScriptable debugPowerUp;
    public CharacterDataScriptable autoRestartCharacter;
    
    
    // whenever you add a new cheat make sure to add it to the auto disable are below!
    public bool debugNoRegularSpawns = false;
    public bool instantEnterPlayMode = false;
    public bool playerIsImmune;
    public bool playerDealMaxDamage;
    public bool everyPathIsEncounter = false;

    public bool autoPlayTest = false;

    public bool setInstantEnterShopAnimation = false;

    public bool overrideTrainState = false;
    
    public DataSaver.TrainState trainState;

    private void Awake() {
        if (!Application.isEditor) {
            ResetDebugOptions();
        }
    }

    public void ResetDebugOptions() {
        Debug.LogError("Debug Options Reset!");
        debugNoRegularSpawns = false;
        instantEnterPlayMode = false;
        playerIsImmune= false;
        everyPathIsEncounter = false;
        playerDealMaxDamage = false;
        autoPlayTest = false;
        setInstantEnterShopAnimation = false;
        overrideTrainState = false;
    }

    [Button]
    void EnterMissionRewardsArea() {
        DataSaver.s.GetCurrentSave().isInEndRunArea = true;
        DataSaver.s.GetCurrentSave().endRunAreaInfo = new DataSaver.EndRunAreaInfo();
    }
    
    private void Start() {
        if (Application.isEditor) {
            if (debugNoRegularSpawns  || instantEnterPlayMode ||playerIsImmune
                  || everyPathIsEncounter  || setInstantEnterShopAnimation || overrideTrainState)
                Debug.LogError("Debug options active! See _CheatsController for more info");


            var tweakables = TweakablesMaster.s.myTweakables;
            if (tweakables.enemyDamageMultiplier != 1 || tweakables.enemyFirerateBoost != 1 || tweakables.playerDamageMultiplier != 1 || tweakables.playerFirerateBoost != 1 || tweakables.playerAmmoUseMultiplier != 1) {
                Debug.Log("Tweakables values not all zero. Beware numbers not matching");
            }

            //LevelArchetypeScriptable.everyPathEncounterCheat = everyPathIsEncounter;

            if (overrideTrainState) {
                DataSaver.s.GetCurrentSave().myTrain = trainState;
                DataSaver.s.SaveActiveGame();
            }
            
            if (debugNoRegularSpawns)
                EnemyWavesController.s.debugNoRegularSpawns = true;

            if (instantEnterPlayMode) {
                PlayerPrefs.SetInt(MiniGUI_DisableTutorial.exposedName, 0);
            }

            if (instantEnterPlayMode) {
                Invoke(nameof(QuickStart), 0.01f);
            }

            if (playerDealMaxDamage) {
                TweakablesMaster.s.myTweakables.playerDamageMultiplier = 20;
                TweakablesMaster.s.ApplyTweakableChange();
            }

            if (autoPlayTest) {
                AutoPlaytester.s.StartAutoPlayer(true);
            }

            if (setInstantEnterShopAnimation) {
                Train.showEntryMovement = true;
            }
        }
    }

    void QuickStart() {
        MainMenu.s.QuickStartGame();
        PlayStateMaster.s.OnShopEntered.AddListener(OnShopStateEnteredQuickStart);
    }
    void OnShopStateEnteredQuickStart() {
        PlayStateMaster.s.OnShopEntered.RemoveListener(OnShopStateEnteredQuickStart);
        Invoke(nameof(_OnShopStateEnteredQuickStart),0.01f);
    }

    void _OnShopStateEnteredQuickStart() {
        ShopStateController.s.QuickStart();
    }
    
    private void Update() {
        ModuleHealth.isImmune = playerIsImmune;
    }

    /*[Button]
    public void EngageEncounter() {
        ShopStateController.s.DebugEngageEncounter(debugEncounter.title);
    }*/

    private void OnEnable() {
        cheatButton.action.Enable();
        cheatButton.action.performed += EngageCheat;
    }


    private void OnDisable() {
        cheatButton.action.Enable();
        cheatButton.action.performed -= EngageCheat;
    }

    private void EngageCheat(InputAction.CallbackContext obj) {

        if (!PlayStateMaster.s.isCombatStarted()) {
            if (PlayStateMaster.s.isMainMenu())
                MainMenu.s.StartGame();

            ShopStateController.s.QuickStart();

            //DataSaver.s.GetCurrentSave().currentRun.money += 10000;
        } else if (!PlayStateMaster.s.isCombatFinished()) {
            //MoneyController.s.AddScraps(1000);
            
            
            SpeedController.s.TravelToMissionEndDistance(true);

            //MissionWinFinisher.s.MissionWon();

            /*var train = Train.s;
            var healths = train.GetComponentsInChildren<ModuleHealth>();

            foreach (var gModuleHealth in healths) {
                gModuleHealth.DealDamage(gModuleHealth.currentHealth/2);
            }*/
        } 

    }

}
