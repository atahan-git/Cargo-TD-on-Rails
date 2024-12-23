using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CheatsController : MonoBehaviour {
    public static CheatsController s;
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

    public bool forceDisableFastForward = false;
    public bool trainDoesntLoseSteam = false;
    
    public bool instantRepair = false;

    public bool stopMoving = false;
    
    public DataSaver.TrainState trainState;

    private void Awake() {
        s = this;
        if (!Application.isEditor) {
            ResetDebugOptions();
        }
    }

    public void ResetDebugOptions() {
        if(Application.isEditor)
            Debug.LogError("Debug Options Reset!");
        debugNoRegularSpawns = false;
        instantEnterPlayMode = false;
        playerIsImmune= false;
        everyPathIsEncounter = false;
        playerDealMaxDamage = false;
        autoPlayTest = false;
        setInstantEnterShopAnimation = false;
        overrideTrainState = false;
        forceDisableFastForward = false;
        trainDoesntLoseSteam = false;
        instantRepair = false;
    }

    
    private void Start() {
        if (Application.isEditor) {
            if (debugNoRegularSpawns  || instantEnterPlayMode ||playerIsImmune
                  || everyPathIsEncounter  || setInstantEnterShopAnimation || overrideTrainState || forceDisableFastForward || instantRepair)
                Debug.LogError("Debug options active! See _CheatsController for more info");


            var tweakables = TweakablesMaster.s.myTweakables;
            if (tweakables.enemyDamageMultiplier != 1 || tweakables.enemyFirerateBoost != 1 || tweakables.playerDamageMultiplier != 1 || tweakables.playerFirerateBoost != 1 || tweakables.playerAmmoUseMultiplier != 1) {
                Debug.Log("Tweakables values not all zero. Beware numbers not matching");
            }

            if (instantRepair) {
                tweakables.playerRepairTimeMultiplier = 0.01f;
            }

            if (forceDisableFastForward) {
                TimeController.s.debugDisableAbilityToFastForward = true;
            }

            //LevelArchetypeScriptable.everyPathEncounterCheat = everyPathIsEncounter;

            if (overrideTrainState) {
                DataSaver.s.GetCurrentSave().currentRun.myTrain = trainState.Copy();
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
        ModuleHealth.debugImmune = playerIsImmune;
        GunModule.debugMaxDamage = playerDealMaxDamage;
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
        //Debug.Break();
        InfiniteMapController.s.DebugRemakeMap();

        /*if (PlayStateMaster.s.isCombatInProgress()) {
            MissionLoseFinisher.s.MissionLost(MissionLoseFinisher.MissionLoseReason.abandon);
        }*/
        
        /*if (!PlayStateMaster.s.isCombatStarted()) {
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
            }#1#
        } */

        /*for (int i = 0; i < Train.s.carts.Count; i++) {
            var modHealth = Train.s.carts[i].GetComponent<ModuleHealth>();

            var targetHP = modHealth.GetMaxHealth() / 4f;

            if (modHealth.currentHealth > targetHP) {
                modHealth.DealDamage(modHealth.currentHealth-targetHP);
            }
        }*/
    }


    [Button]
    public void DebugSpawnEnemy(GameObject enemy, GameObject gear) {
        EnemyWavesController.s.SpawnEnemy(enemy,gear, SpeedController.s.currentDistance, false, false);
    }


    [Button]
    public void KillAllEnemies() {
        var enemies = EnemyWavesController.s.GetComponentsInChildren<EnemyHealth>();
        for (int i = 0; i < enemies.Length; i++) {
            enemies[i].DealDamage(10000, null, null);
        }
    }


    [Button]
    public void SetTargetFramerate(int target) {
        
        Application.targetFrameRate = target;
    }

[Button]
    public void SetAct(int act) {
        DataSaver.s.GetCurrentSave().currentRun.currentAct = act;
        DataSaver.s.SaveActiveGame();
    }
}
