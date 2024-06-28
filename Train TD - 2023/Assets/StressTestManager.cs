using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class StressTestManager : MonoBehaviour {

    public bool doStressTest = false;
    public GameObject startStation;
    // Start is called before the first frame update
    void Start() {
        if (doStressTest) {
            Invoke(nameof(BeginStressTest), 0.5f);
        }
    }

    public bool stressTestActive = false;

    public DataSaver.TrainState stressTestTrain;

    public int stressTestMultiplier = 1;
    
    [Button]
    void BeginStressTest() {
        stressTestActive = true;
        startStation.SetActive(false);
        Train.s.DrawTrain(stressTestTrain);
        PlayStateMaster.s.DebugSetGameState(PlayStateMaster.GameState.combat);
        ModuleHealth.prologueIndestructable = true;
        EnemyTargetAssigner.s.shootCreditPerSecond = 10;
        EnemyTargetAssigner.s.maxShootCredit = 20;
        CheatsController.s.trainDoesntLoseSteam = true;
        
        
        // other stuff
        MapController.s.MakeNewMap(); 
        PlayStateMaster.s.ClearLevel();
        MainMenu.s.ExitMainMenu();
        Pauser.s.Unpause();
        PlayerWorldInteractionController.s.Deselect();
        WorldDifficultyController.s.overrideDifficulty = true;
        WorldDifficultyController.s.OnCombatStart();
        VignetteController.s.ResetVignette();
        ScreenFadeToWhiteController.s.ResetFadeToWhite();
        LevelReferences.s.ClearCombatHoldableThings();
        SpeedController.s.ResetDistance();
        PathSelectorController.s.enabled = false;
        TimeController.s.OnCombatStart();
        PlayerWorldInteractionController.s.OnEnterCombat();
        ShopStateController.s.starterUI.SetActive(false);
        ShopStateController.s.mapOpenButton.interactable = false;
        GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.openMap);
        ShopStateController.s.gameUI.SetActive(true);
        RangeVisualizer.SetAllRangeVisualiserState(true);
        PlayerWorldInteractionController.s.canSelect = true;
        PlayStateMaster.s.SetCurrentLevel(DataHolder.s.levelArchetypeScriptables[DataSaver.s.GetCurrentSave().currentRun.currentAct-1].GenerateLevel());
        PathAndTerrainGenerator.s.MakeCircleTerrainForPrologue();
        MapController.s.enabled = false;
        SpeedController.s.currentDistance = 50;
        Invoke(nameof(SetTrainPressure), 2f);

        Invoke(nameof(SpawnEnemies),2f);
    }

    void SpawnEnemies() {
        for (int i = 0; i < stressTestMultiplier; i++) {
            EnemyWavesController.s.MakeSegmentEnemy(60,new UpgradesController.PathEnemyType(){myType =  UpgradesController.PathEnemyType.PathType.regular},true, true, true, true);
            EnemyWavesController.s.MakeSegmentEnemy(60,new UpgradesController.PathEnemyType(){myType =  UpgradesController.PathEnemyType.PathType.regular},true, false, true, true);
        }
    }

    void SetTrainPressure() {
        var engine = Train.s.GetComponentInChildren<EngineModule>();
        engine.currentPressure = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (stressTestActive) {
            /*if (EnemyWavesController.s.GetActiveEnemyCount() <= 5*stressTestMultiplier) {
                EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType() { myType = UpgradesController.PathEnemyType.PathType.regular },
                    true, true, true, true);
                EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType() { myType = UpgradesController.PathEnemyType.PathType.regular },
                    true, false, true, true);
            }*/

            if (Train.s.GetComponent<AmmoTracker>().ammoProviders[0].AvailableAmmo() < 2) {
                (Train.s.GetComponent<AmmoTracker>().ammoProviders[0] as ModuleAmmo).Reload(20);
            }
        }
    }
}
