using System;
using System.Collections;
using System.Collections.Generic;
using ConversationSystem;
using HighlightPlus;
using UnityEngine;
using Random = UnityEngine.Random;

public class PrologueController : MonoBehaviour {
    public static PrologueController s;

    private void Awake() {
        s = this;
        tutorialUI.SetActive(false);
    }
    

    public GameObject tutorialUI;
    public GameObject prologueStartingPrefab;
    void Start() {
        ModuleHealth.prologueIndestructable = false;
    }

    private DataSaver.TutorialProgress _progress => DataSaver.s.GetCurrentSave().tutorialProgress;


    public bool isPrologueActive = false;

    public PrologueState currentState;
    
    public GameObject[] tutorialPanels;

    void ChangeIntoState(PrologueState targetState) {
        currentState.ExitState();
        targetState.tutorialPanels = tutorialPanels;
        targetState.EnterState();
        currentState = targetState;


        if (targetState is GetGun) {
            StartCoroutine(GetGunAnimations());
        }else if (targetState is TimeToDie) {
            StartCoroutine(TimeToDieAnimations());
        }
    }

    class WaitingLoad : PrologueState {
        public override void EnterState() {}

        public override PrologueState UpdateState() {
            return null;
        }
        public override void OnSelectSomething(IPlayerHoldable holdable, bool isSelecting) {}
        public override void OnDirectControlStateChange(bool isEngaged) {}
        public override void ExitState() {}
    }
    class NeedRepairs : PrologueState {
        private float stateTime = 0;
        private bool higlightingDrone = false;
        private bool higlightingEngine = false;
        private bool canExitRepairsActive = false;
        public override void EnterState() {
            var engineHealth = Train.s.carts[0].GetHealthModule();
            Random.InitState(42);
            engineHealth.DealDamage(engineHealth.GetMaxHealth()*10);

            var repairModule = Train.s.carts[1].GetComponentInChildren<DroneRepairController>();
            repairModule.DisableAutoDrone();
            repairModule.droneCannotActivateOverride = true;
            
            
            //tutorialPanels[0].SetActive(true);
            ConversationsHolder.s.TriggerConversation(ConversationsIds.Prologue_0__meteor_repair_engine, 2.5f);
        }

        public override PrologueState UpdateState() {
            stateTime += Time.deltaTime;
            if (stateTime > 45 && !higlightingDrone) {
                HighlightDirectControlRepairDrone();
            }

            if (stateTime > 75 && !higlightingEngine) {
                HighlightEngine();
            }
            
            var engine = Train.s.carts[0];

            if (!engine.isDestroyed ) {
                if (DirectControlMaster.s.directControlInProgress) {
                    if (!canExitRepairsActive) {
                        canExitRepairsActive = true;
                        ConversationsHolder.s.TriggerConversation(ConversationsIds.Prologue_1__cart_activate_after_half,0.5f);
                        //tutorialPanels[1].SetActive(true);
                    }
                } else {
                    ConversationsHolder.s.TriggerConversation(ConversationsIds.Prologue_2__repair_drone_auto_repair,1f);
                    //tutorialPanels[2].SetActive(true);
                    return new StartEngine();
                }
            }
            return null;
        }

        public override void OnSelectSomething(IPlayerHoldable holdable, bool isSelecting) {
            if (!isSelecting) {
                if (higlightingDrone) {
                    var repairModule = Train.s.carts[1];

                    if (repairModule == PlayerWorldInteractionController.s.currentSelectedThing) {
                        return;
                    }

                    HighlightEffect outline = null;
                    outline = repairModule.GetComponentInChildren<HighlightEffect>();

                    if (outline != null) {
                        outline.outlineColor = PlayerWorldInteractionController.s.repairColor;
                        outline.highlighted = true;
                    }
                }
            }
        }

        public override void OnDirectControlStateChange(bool isEngaged) {
            if (higlightingEngine) {
                var engineModule = Train.s.carts[0];

                if (engineModule == PlayerWorldInteractionController.s.currentSelectedThing) {
                    return;
                }

                HighlightEffect outline = null;
                outline = engineModule.GetComponentInChildren<HighlightEffect>();

                if (outline != null) {
                    outline.outlineColor = PlayerWorldInteractionController.s.repairColor;
                    outline.highlighted = isEngaged;
                }
            }
        }

        void HighlightDirectControlRepairDrone() {
            higlightingDrone = true;
        
            var repairModule = Train.s.carts[1];

            if (repairModule == PlayerWorldInteractionController.s.currentSelectedThing || DirectControlMaster.s.directControlInProgress) {
                return;
            }
        
            HighlightEffect outline = null;
            outline = repairModule.GetComponentInChildren<HighlightEffect>();

            if (outline != null) {
                outline.outlineColor = PlayerWorldInteractionController.s.repairColor;
                outline.highlighted = true;
            }
        }

        void HighlightEngine() {
            higlightingEngine = true;
        
            var engineModule = Train.s.carts[1];

            if (engineModule == PlayerWorldInteractionController.s.currentSelectedThing || !DirectControlMaster.s.directControlInProgress) {
                return;
            }
        
            HighlightEffect outline = null;
            outline = engineModule.GetComponentInChildren<HighlightEffect>();

            if (outline != null) {
                outline.outlineColor = PlayerWorldInteractionController.s.repairColor;
                outline.highlighted = true;
            }
        }
        public override void ExitState() {}
    }

    class StartEngine : PrologueState {
        private float stateTime = 0;
        private bool higlightingControlEngine = false;
        private bool showEnginePowerTutorial = false;
        public override void EnterState() {
            DirectControlMaster.s.DisableDirectControl();
            var repairModule = Train.s.carts[1].GetComponentInChildren<DroneRepairController>();
            repairModule.droneCannotActivateOverride = false;
            repairModule.ActivateAutoDrone();
        
            RemoveAllHighlights();
        }


        public override PrologueState UpdateState() {
            stateTime += Time.deltaTime;
            var engine = Train.s.carts[0].GetComponentInChildren<EngineModule>();
            
            if (!higlightingControlEngine && stateTime > 30) {
                HighlightControlEngine();
            }

            if (!showEnginePowerTutorial && DirectControlMaster.s.currentDirectControllable == Train.s.carts[0].GetComponentInChildren<IDirectControllable>()) {
                showEnginePowerTutorial = true;
                //tutorialPanels[3].SetActive(true);
                ConversationsHolder.s.TriggerConversation(ConversationsIds.Prologue_3__put_fuel, 0.5f);
            }
            
            if (engine.GetEffectivePressure() > 1.8f) {
                //tutorialPanels[4].SetActive(true);
                ConversationsHolder.s.TriggerConversation(ConversationsIds.Prologue_4__cannot_go_to_red, 0.1f);
                engine.currentPressure = 1.1f;
            }

            if (engine.GetEffectivePressure() >= 1f && !DirectControlMaster.s.directControlInProgress) {
                return new GetGun();
            }

            return null;
        }
        
        
        void HighlightControlEngine() {
            higlightingControlEngine = true;
        
            var engineModule = Train.s.carts[0];

            if (engineModule == PlayerWorldInteractionController.s.currentSelectedThing || DirectControlMaster.s.directControlInProgress) {
                return;
            }

            HighlightEffect outline = null;
            outline = engineModule.GetComponentInChildren<HighlightEffect>();

            if (outline != null) {
                outline.outlineColor = PlayerWorldInteractionController.s.engineBoostColor;
                outline.highlighted = true;
            }
        }

        public override void OnSelectSomething(IPlayerHoldable holdable, bool isSelecting) {
            if (!isSelecting) {
                if (higlightingControlEngine) {
                    var engineModule = Train.s.carts[0];

                    if (engineModule == PlayerWorldInteractionController.s.currentSelectedThing) {
                        return;
                    }

                    HighlightEffect outline = null;
                    outline = engineModule.GetComponentInChildren<HighlightEffect>();

                    if (outline != null) {
                        outline.outlineColor = PlayerWorldInteractionController.s.engineBoostColor;
                        outline.highlighted = true;
                    }
                }
            }
        }

        public override void OnDirectControlStateChange(bool isEngaged) {
            if (higlightingControlEngine) {
                var engineModule = Train.s.carts[0];

                if (engineModule == PlayerWorldInteractionController.s.currentSelectedThing) {
                    return;
                }

                HighlightEffect outline = null;
                outline = engineModule.GetComponentInChildren<HighlightEffect>();

                if (outline != null) {
                    outline.outlineColor = PlayerWorldInteractionController.s.engineBoostColor;
                    outline.highlighted = !isEngaged;
                }
            }
        }

        public override void ExitState() {
            
        }
    }
    
    class GetGun : PrologueState {
        public float funkyCamMagnitude = 0;
        Matrix4x4 originalProjection;
        Camera cam;
        public override void EnterState() {
            cam = MainCameraReference.s.cam;
            originalProjection = cam.projectionMatrix;
        }

        public override PrologueState UpdateState() {
            
            Matrix4x4 p = originalProjection;
            p.m01 += Mathf.Sin(Time.time * 1.2F) * 0.1F * funkyCamMagnitude;
            p.m10 += Mathf.Sin(Time.time * 1.5F) * 0.1F * funkyCamMagnitude;
            cam.projectionMatrix = p;

            CheckRewardCartLost(PrologueController.s.cannonState);
                
            if (Train.s.carts.Count >= 3 && !PlayerWorldInteractionController.s.isDragging()) {
                return new GetAmmoAndKillOneEnemy();
            }

            return null;
        }

        public override void OnSelectSomething(IPlayerHoldable holdable, bool isSelecting) {
            
        }

        public override void OnDirectControlStateChange(bool isEngaged) {
            
        }

        public override void ExitState() {
            cam.projectionMatrix = originalProjection;
        }
    }
    public GameObject friendlyEnemyBattalion;
    public GameObject friendlyEnemyBattalionBigger;
    class GetAmmoAndKillOneEnemy : PrologueState {
        private float stateTime;
        public bool enemyHasSpawned;
        private bool shownShootTutorial;
        public override void EnterState() {
            var gunModules = Train.s.GetComponentsInChildren<GunModule>();
            foreach (var gun in gunModules) {
                gun.DeactivateGun();
                gun.gunCannotActivateOverride = true;
            }
            
            tutorialPanels[8].SetActive(true);
        }

        void SpawnAmmoCartAndFriendlyEnemy() {
            SpawnNewRewardCart(s.ammoCartState);
            rewardCart.GetComponentInChildren<ModuleAmmo>().SetAmmo(0);
            EnemyWavesController.s.SpawnCustomBattalion(s.friendlyEnemyBattalion, SpeedController.s.currentDistance+30, true, true);

            enemyHasSpawned = true;
            stateTime = 0;
        }

        public override PrologueState UpdateState() {
            stateTime += Time.deltaTime;
            CheckRewardCartLost(s.ammoCartState);

            if (!enemyHasSpawned && stateTime > 0.5f) {
                SpawnAmmoCartAndFriendlyEnemy();
            }

            if (!shownShootTutorial && Train.s.GetComponent<AmmoTracker>().GetAmmoPercent() > 0.001f) {
                shownShootTutorial = true;
                tutorialPanels[9].SetActive(true);
            }
            
            if (enemyHasSpawned && stateTime > 1f && Train.s.carts.Count >= 4) {
                if (EnemyWavesController.s.GetActiveEnemyCount() <= 0) {
                    return new GetArtifactAndKillEnemy();
                }
            }

            return null;
        }

        public override void OnSelectSomething(IPlayerHoldable holdable, bool isSelecting) {
            
        }

        public override void OnDirectControlStateChange(bool isEngaged) {
           
        }

        public override void ExitState() {
            var gunModules = Train.s.GetComponentsInChildren<GunModule>();
            foreach (var gun in gunModules) {
                gun.ActivateGun();
                gun.gunCannotActivateOverride = false;
            }
        }
    }

    public GameObject bigGemRewardOnRoad;
    class GetArtifactAndKillEnemy : PrologueState {
        private float stateTime;
        private bool enemyHasSpawned;
        private float gemDistance;
        private bool gemTaken;
        private bool shownShootTutorial;
        public override void EnterState() {
            tutorialPanels[10].SetActive(true);
            var gemReward = Instantiate(s.bigGemRewardOnRoad).GetComponent<GemRewardOnRoad>();
            gemDistance = SpeedController.s.currentDistance + 45;
            gemReward.autoStart = false;
            gemReward.SetUp(SpeedController.s.currentDistance + 30);
        }

        void SpawnFriendlyEnemy() {
            EnemyWavesController.s.SpawnCustomBattalion(s.friendlyEnemyBattalionBigger, SpeedController.s.currentDistance+30, true, true);
            enemyHasSpawned = true;
            stateTime = 0;
        }

        public override PrologueState UpdateState() {
            stateTime += Time.deltaTime;
            
            if (!enemyHasSpawned && stateTime > 0.5f && SpeedController.s.currentDistance > gemDistance && Train.s.GetComponentsInChildren<Artifact>().Length > 0) {
                tutorialPanels[11].SetActive(true);
                SpawnFriendlyEnemy();
            }

            //print($"Enemy spawned {enemyHasSpawned}");
            if (enemyHasSpawned) {
                if (stateTime > 1f && EnemyWavesController.s.GetActiveEnemyCount() <= 0) {
                    return new TimeToDie();
                }
            }

            return null;
        }

        public override void OnSelectSomething(IPlayerHoldable holdable, bool isSelecting) {
            
        }

        public override void OnDirectControlStateChange(bool isEngaged) {
           
        }

        public override void ExitState() {
            
        }
    }
    
    class TimeToDie : PrologueState {
        
        int reinforcementsCount = 1;
        public bool finalEnemiesSpawned;
        public override void EnterState() {
        }

        public override PrologueState UpdateState() {
            if (finalEnemiesSpawned) {
                if (EnemyWavesController.s.GetActiveEnemyCount() <= 10) {
                    for (int i = 0; i < reinforcementsCount; i++) {
                        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, true);
                        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, false);
                    }

                    reinforcementsCount += 1;
                }
            }

            return null;
        }

        public override void OnSelectSomething(IPlayerHoldable holdable, bool isSelecting) {
            
        }

        public override void OnDirectControlStateChange(bool isEngaged) {
           
        }

        public override void ExitState() {
            
        }
    }
    
    
    public void EngagePrologue() {
        PlayStateMaster.s.EnterPrologue();
        PlayerWorldInteractionController.s.OnSelectSomething.AddListener(OnSelectSomething);
        DirectControlMaster.s.OnDirectControlStateChange.AddListener(OnDirectControlStateChange);
        currentState = new WaitingLoad();
        DataSaver.s.GetCurrentSave().tutorialProgress.runsMadeAfterTutorial = -1;
        DataSaver.s.GetCurrentSave().runsMade = -1;
        isPrologueActive = true;

        for (int i = 0; i < tutorialPanels.Length; i++) {
            tutorialPanels[i].SetActive(false);
        }
        
        tutorialUI.SetActive(true);
    }

    public void DrawPrologueWorld() {
        BiomeController.s.SetParticularBiome(0,0);
        PathAndTerrainGenerator.s.MakeCircleTerrainForPrologue();
        PathSelectorController.s.trainStationStart.SetActive(false);
    }

    public DataSaver.TrainState prologueTrain;
    public void PrologueLoadComplete() {
        Train.s.DrawTrain(prologueTrain);
        
        WakeUpAnimation.s.Engage();

        ChangeIntoState(new NeedRepairs());

        AsteroidInTheDistancePositionController.s.centerMode = true;

        var startDistance = 50;
        
        Instantiate(prologueStartingPrefab, PathAndTerrainGenerator.s.GetPointOnActivePath(startDistance), PathAndTerrainGenerator.s.GetRotationOnActivePath(startDistance));
        
        //cleanup stuff
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
        MapController.s.enabled = false;
        SpeedController.s.currentDistance = startDistance;
        PlayerWorldInteractionController.s.canSelect = true;
        PlayStateMaster.s.SetCurrentLevel(DataHolder.s.levelArchetypeScriptables[0].GenerateLevel());
    }
    

    private void OnSelectSomething(IPlayerHoldable holdable, bool isSelecting) {
        currentState.OnSelectSomething(holdable, isSelecting);
    }

    void OnDirectControlStateChange(bool isEngaged) {
        currentState.OnDirectControlStateChange(isEngaged);
    }


    private void Update() {
        if (isPrologueActive) {
            var result = currentState.UpdateState();
            if (result != null) {
                ChangeIntoState(result);
            }
        }
    }

    IEnumerator TimeToDieAnimations() {
        yield return new WaitForSeconds(2.5f);
        
        var meteorShard = Train.s.carts[0].GetComponentInChildren<Artifact_MeteorShard>();
        meteorShard.SetParticlesState(true);
        
        tutorialPanels[12].SetActive(true);
        
        yield return new WaitForSeconds(2f);
        
        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, true);
        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, true);
        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, false);
        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, false);
        (currentState as TimeToDie).finalEnemiesSpawned = true;
        ModuleHealth.prologueIndestructable = false;
        
        var allGuns = EnemyWavesController.s.GetComponentsInChildren<EnemyGunModule>();
        var n = 0;
        foreach (var gun in allGuns) {
            gun.isUniqueGearNoNeedForShootCredit = true;
        }

        WorldDifficultyController.s.overrideDifficulty = true;
        WorldDifficultyController.s.curLevel = 50;
        WorldDifficultyController.s.CalculateDifficulty();

        DataSaver.s.GetCurrentSave().instantRestart = true;
        
        SetBiome(2);
        yield return new WaitForSeconds(0.05f);
        EnemyWavesController.s.gameObject.SetActive(false);
        SetBiome(0);
        yield return new WaitForSeconds(0.05f);
        EnemyWavesController.s.gameObject.SetActive(true);
        SetBiome(2);
        yield return new WaitForSeconds(0.1f);
        EnemyWavesController.s.gameObject.SetActive(false);
        SetBiome(0);
        yield return new WaitForSeconds(0.1f);
        EnemyWavesController.s.gameObject.SetActive(true);
        SetBiome(2);
        yield return new WaitForSeconds(0.25f);
        EnemyWavesController.s.gameObject.SetActive(false);
        SetBiome(0);
        yield return new WaitForSeconds(0.25f);
        EnemyWavesController.s.gameObject.SetActive(true);
        SetBiome(2);
        yield return new WaitForSeconds(0.5f);
        EnemyWavesController.s.gameObject.SetActive(false);
        SetBiome(0);
        yield return new WaitForSeconds(0.5f);
        EnemyWavesController.s.gameObject.SetActive(true);
        SetBiome(2);
    }

    public DataSaver.TrainState.CartState cannonState;
    public DataSaver.TrainState.CartState ammoCartState;
    

    public GameObject cartRewardChest;
    IEnumerator GetGunAnimations() {
        yield return new WaitForSeconds(2f);
        
        //tutorialPanels[5].SetActive(true);
        ConversationsHolder.s.TriggerConversation(ConversationsIds.Prologue_5__oh_no_artifact,0.5f);
        
        var curState = currentState as GetGun;
        curState.funkyCamMagnitude = 0;

        while (curState.funkyCamMagnitude < 1f) {
            curState.funkyCamMagnitude += 1 * Time.deltaTime;
            if (curState.funkyCamMagnitude > 1f)
                curState.funkyCamMagnitude = 1;
            yield return null;
        }


        var meteorShard = Train.s.carts[0].GetComponentInChildren<Artifact_MeteorShard>();
        meteorShard.SetParticlesState(true);

        /*var engine = Train.s.carts[0].GetComponentInChildren<EngineModule>();
        engine.currentPressure = 1.2f;*/
        yield return new WaitForSeconds(2f);
        
        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, true);
        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, true);
        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, false);
        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, false);
        ModuleHealth.prologueIndestructable = true;
        
        SetBiome(1);
        yield return new WaitForSeconds(0.05f);
        EnemyWavesController.s.gameObject.SetActive(false);
        SetBiome(0);
        yield return new WaitForSeconds(0.05f);
        EnemyWavesController.s.gameObject.SetActive(true);
        SetBiome(1);
        yield return new WaitForSeconds(0.1f);
        EnemyWavesController.s.gameObject.SetActive(false);
        SetBiome(0);
        yield return new WaitForSeconds(0.1f);
        EnemyWavesController.s.gameObject.SetActive(true);
        SetBiome(1);
        yield return new WaitForSeconds(0.25f);
        EnemyWavesController.s.gameObject.SetActive(false);
        SetBiome(0);
        yield return new WaitForSeconds(0.25f);
        EnemyWavesController.s.gameObject.SetActive(true);
        SetBiome(1);
        yield return new WaitForSeconds(0.5f);
        EnemyWavesController.s.gameObject.SetActive(false);
        SetBiome(0);
        yield return new WaitForSeconds(0.5f);
        EnemyWavesController.s.gameObject.SetActive(true);
        SetBiome(1);
        
        yield return new WaitForSeconds(2f);

        var timeRemaining = 3f;
        
        var allGuns = EnemyWavesController.s.GetComponentsInChildren<EnemyGunModule>();
        var n = 0;
        foreach (var gun in allGuns) {
            gun.StopShooting();
            gun.ShootBarrageDebug();

            timeRemaining -= Time.deltaTime;
            yield return null;
            
            n += 1;
            if (n >= 5) {
                n = 0;
                yield return new WaitForSeconds(0.1f);
            }

            if (timeRemaining <= 0) {
                break;
            }
        }

        if (timeRemaining < 1f) {
            timeRemaining = 1f;
        }
        yield return new WaitForSeconds(timeRemaining);
        EnemyWavesController.s.Cleanup();
        EnemyWavesController.s.enemiesInitialized = true;
        SetBiome(0);
        
        meteorShard.SetParticlesState(false);
        
        var rewardChest = Instantiate(cartRewardChest);
        var rewardDistance = SpeedController.s.currentDistance + 60;
        var rewardChestScript = rewardChest.GetComponent<CartRewardOnRoad>();
        rewardChestScript.SetUp(rewardDistance);
        rewardChestScript.OnCustomRewardSpawned.AddListener(curState.RegisterRewardCart);
        
        curState.funkyCamMagnitude = 1;

        while (curState.funkyCamMagnitude > 0f) {
            curState.funkyCamMagnitude -= 0.5f * Time.deltaTime;
            if (curState.funkyCamMagnitude < 0f)
                curState.funkyCamMagnitude = 0;
            yield return null;
        }
        
        
        tutorialPanels[6].SetActive(true);


        while (SpeedController.s.currentDistance < rewardDistance-2.5f) {
            yield return null;
        }
        
        TimeController.s.SetSlowDownAndPauseState(true);
        
        tutorialPanels[7].SetActive(true);

        yield return new WaitForSecondsRealtime(2f);
        
        TimeController.s.SetSlowDownAndPauseState(false);
    }

    private float defaultDetailDistance = -1f;
    private float defaultTreeDistance = -1f;
    void SetBiome(int biome) {
        var targetTexture = PathAndTerrainGenerator.s.biomes[biome].terrainPrefab.GetComponent<Terrain>().materialTemplate;
        var allTerrains = PathAndTerrainGenerator.s.terrainPool.GetAllObjs();

        if (defaultDetailDistance < 0) {
            var terrain = allTerrains[0].GetComponent<Terrain>();
            defaultDetailDistance = terrain.detailObjectDistance;
            defaultTreeDistance = terrain.treeDistance;
        }
        
        for (int i = 0; i < allTerrains.Length; i++) {
            var curTerrain = allTerrains[i].GetComponent<Terrain>();
            curTerrain.materialTemplate = targetTexture;
            if (biome == 0) {
                curTerrain.detailObjectDistance = defaultDetailDistance;
                curTerrain.treeDistance = defaultTreeDistance;
            } else {
                curTerrain.detailObjectDistance = 0f;
                curTerrain.treeDistance = 0f;
            }
        }
        
        BiomeController.s.SetParticularBiome(0,biome,false);
    }


    public void PrologueDone() {
        isPrologueActive = false;
    }

[Serializable]
    public abstract class PrologueState {
        public GameObject[] tutorialPanels;
        public abstract void EnterState();
        public abstract PrologueState UpdateState();
        public abstract void OnSelectSomething(IPlayerHoldable holdable, bool isSelecting);
        public abstract void OnDirectControlStateChange(bool isEngaged);
        public abstract void ExitState();
        
        protected void RemoveAllHighlights() {
            for (int i = 0; i < Train.s.carts.Count; i++) {
                var cart = Train.s.carts[i];
                if (cart == PlayerWorldInteractionController.s.currentSelectedThing) {
                    continue;
                }
                HighlightEffect outline = null;
                outline = cart.GetComponentInChildren<HighlightEffect>();

                if (outline != null) {
                    outline.highlighted = false;
                }
            }
        }
        
        public GameObject rewardCart;
        private bool rewardCartSpawned = false;
        public void RegisterRewardCart(GameObject cart) {
            rewardCart = cart;
            rewardCartSpawned = true;
        }
        
        protected void CheckRewardCartLost(DataSaver.TrainState.CartState rewardCartState) {
            if (rewardCartSpawned) {
                if (rewardCart == null) {
                    RewardCartLost(rewardCartState);
                }

                /*if (Vector3.Distance(rewardCart.transform.position, Train.s.trainMiddle.position) > 40) {
                    RewardCartLost(rewardCartState);
                }*/
            }
        }

        void RewardCartLost(DataSaver.TrainState.CartState rewardCartState) {
            WakeUpAnimation.s.Engage();
            DirectControlMaster.s.DisableDirectControl();
            CameraController.s.ResetCameraPos();
        
            SpawnNewRewardCart(rewardCartState);
        }
        
        protected void SpawnNewRewardCart(DataSaver.TrainState.CartState rewardCartState) {
            var target = Train.s.trainFront;
            var cart = Instantiate(DataHolder.s.GetCart(rewardCartState.uniqueName).gameObject, target.position+Vector3.up/2f, target.rotation).GetComponent<Cart>();
            Train.ApplyStateToCart(cart, rewardCartState);
            VisualEffectsController.s.SmartInstantiate(LevelReferences.s.goodItemSpawnEffectPrefab,  target.position+Vector3.up/2f, target.rotation);

            var rg = cart.GetComponent<Rigidbody>();
            rg.isKinematic = false;
            rg.useGravity = true;
            rg.velocity = Train.s.GetTrainForward() * LevelReferences.s.speed;
            rg.AddForce(StopAndPick3RewardUIController.s.GetUpForce());
        
            LevelReferences.s.combatHoldableThings.Add(cart);

            RegisterRewardCart(cart.gameObject);
        }
    }
}
