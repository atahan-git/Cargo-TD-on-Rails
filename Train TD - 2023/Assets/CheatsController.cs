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
    public bool infiniteLevel = false;
    public bool debugNoRegularSpawns = false;
    public bool instantEnterPlayMode = false;
    public bool playerIsImmune;
    public bool restartOnStart = false;
    public bool autoRestartWithSelectedCharacter = false;
    public bool dontDrawMap = false;
    
    public EnemyIdentifier debugEnemy;

    private void Start() {
#if !UNITY_EDITOR
        infiniteLevel = false;
        debugNoRegularSpawns = false;
        instantEnterPlayMode = false;
        playerIsImmune= false;
        restartOnStart = false;
        autoRestartWithSelectedCharacter = false;
        dontDrawMap = false;
#endif
        
        /*if(debugNoRegularSpawns || infiniteLevel || infiniteLevel || instantEnterPlayMode || playerIsImmune || restartOnStart || dontDrawMap || autoRestartWithSelectedCharacter)
            Debug.LogError("Debug options active! See _CheatsController for more info");
        
        if (debugNoRegularSpawns)
            EnemyWavesController.s.debugNoRegularSpawns = true;

        if (restartOnStart)
            DataSaver.s.GetCurrentSave().isInARun = false;
        
        if(dontDrawMap)
            WorldMapCreator.s.QuickStartNoWorldMap();

        if (autoRestartCharacter || autoRestartWithSelectedCharacter) {
            PlayerPrefs.SetInt(MiniGUI_DisableTutorial.exposedName, 0);
        }

        if (autoRestartWithSelectedCharacter) {
            Invoke(nameof(QuickRestartWithCheaterCharacter),0.01f);
        }else if (instantEnterPlayMode) {
           Invoke(nameof(QuickStart),0.01f);
        }*/
    }

    void QuickStart() {
        if(dontDrawMap)
            WorldMapCreator.s.QuickStartNoWorldMap();
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
    
    
    
    void QuickRestartWithCheaterCharacter() {
        DataSaver.s.GetCurrentSave().isInARun = false;
        if(dontDrawMap)
            WorldMapCreator.s.QuickStartNoWorldMap();
        MainMenu.s.QuickStartGame();
        PlayStateMaster.s.OnOpenCharacterSelectMenu.AddListener(OnShopStateEnteredQuickRestartWithCheaterCharacter);
    }
    
    void OnShopStateEnteredQuickRestartWithCheaterCharacter() {
        CharacterSelector.s.SelectCharacter(autoRestartCharacter.myCharacter);
        PlayStateMaster.s.OnOpenCharacterSelectMenu.RemoveListener(OnShopStateEnteredQuickRestartWithCheaterCharacter);

        if (instantEnterPlayMode) {
            Invoke(nameof(_OnShopStateEnteredQuickStart),0.5f);
        }
    }


    private void Update() {
        if (infiniteLevel) {
            if (SpeedController.s.missionDistance - SpeedController.s.currentDistance < 50) {
                SpeedController.s.IncreaseMissionEndDistance(50);
            }
        }
        
        ModuleHealth.isImmune = playerIsImmune;
    }

    [Button]
    void DebugEnemySpawn(int distance) {
        EnemyWavesController.s.DebugEnemySpawn(debugEnemy, distance);
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
            if (WorldMapCreator.s.worldMapOpen) {
                MapController.s.DebugTravelToSelectStar();
                
            } else {
                if (PlayStateMaster.s.isMainMenu())
                    MainMenu.s.StartGame();

                ShopStateController.s.QuickStart();

                //DataSaver.s.GetCurrentSave().currentRun.money += 10000;
            }
        } else if (!PlayStateMaster.s.isCombatFinished()) {
            //MoneyController.s.AddScraps(1000);
            
            
            SpeedController.s.TravelToMissionEndDistance();

            //MissionWinFinisher.s.MissionWon();

            /*var train = Train.s;
            var healths = train.GetComponentsInChildren<ModuleHealth>();

            foreach (var gModuleHealth in healths) {
                gModuleHealth.DealDamage(gModuleHealth.currentHealth/2);
            }*/
        } 

    }
}
