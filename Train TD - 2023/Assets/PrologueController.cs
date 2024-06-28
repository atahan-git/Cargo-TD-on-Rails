using System;
using System.Collections;
using System.Collections.Generic;
using HighlightPlus;
using UnityEngine;
using Random = UnityEngine.Random;

public class PrologueController : MonoBehaviour {
    public static PrologueController s;

    private void Awake() {
        s = this;
    }
    
    Matrix4x4 originalProjection;
    Camera cam;

    public GameObject prologueStartingPrefab;
    void Start() {
        ModuleHealth.prologueIndestructable = false;
        cam = MainCameraReference.s.cam;
        originalProjection = cam.projectionMatrix;
    }

    private DataSaver.TutorialProgress _progress => DataSaver.s.GetCurrentSave().tutorialProgress;
    
    
    public enum PrologueState {
        waitingLoad, needRepairs, startEngine, getGun, getAmmoAndKillOneEnemy, timeToDie
    }

    public PrologueState currentState;
    
    

    [SerializeField]
    public class PrologueStateValues {
        public bool higlightingDrone = false;
        public bool higlightingEngine = false;
        public bool higlightingControlEngine = false;
        public bool rewardCartSpawned = false;
        public bool enemyHasSpawned = false;
        public bool finalEnemiesSpawned = false;
    }

    public PrologueStateValues currentValues;
    
    
    public void EngagePrologue() {
        PlayStateMaster.s.EnterPrologue();
        PlayerWorldInteractionController.s.OnSelectSomething.AddListener(OnSelectSomething);
        DirectControlMaster.s.OnDirectControlStateChange.AddListener(OnDirectControlStateChange);
        currentState = PrologueState.waitingLoad;
        currentValues = new PrologueStateValues();
    }

    public void DrawPrologueWorld() {
        PathAndTerrainGenerator.s.SetBiomes(0);
        PathAndTerrainGenerator.s.MakeCircleTerrainForPrologue();
        PathSelectorController.s.trainStationStart.SetActive(false);
    }

    public DataSaver.TrainState prologueTrain;
    public void PrologueLoadComplete() {
        Train.s.DrawTrain(prologueTrain);
        var engineHealth = Train.s.carts[0].GetHealthModule();
        engineHealth.DealDamage(engineHealth.GetMaxHealth());

        var repairModule = Train.s.carts[1].GetComponentInChildren<DroneRepairController>();
        repairModule.DisableAutoDrone();
        repairModule.droneCannotActivateOverride = true;
        
        WakeUpAnimation.s.Engage();
        Invoke(nameof(HighlightDirectControlRepairDrone), 45f);
        Invoke(nameof(HighlightEngine), 60f+15f);

        currentState = PrologueState.needRepairs;

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
        switch (currentState) {
            case PrologueState.needRepairs: {
                if (!isSelecting) {
                    if (currentValues.higlightingDrone) {
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
                break;
            case PrologueState.startEngine: {
                if (!isSelecting) {
                    if (currentValues.higlightingControlEngine) {
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
                break;
        }
    }

    void OnDirectControlStateChange(bool isEngaged) {
        switch (currentState) {
            case PrologueState.needRepairs: {
                if (currentValues.higlightingEngine) {
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
                break;
            case PrologueState.startEngine: {
                    if (currentValues.higlightingControlEngine) {
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
                break;
                
        }
    }


    void HighlightDirectControlRepairDrone() {
        currentValues.higlightingDrone = true;
        if (currentState != PrologueState.needRepairs) {
            return;
        }
        
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
        currentValues.higlightingEngine = true;
        if (currentState != PrologueState.needRepairs) {
            return;
        }
        
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


    private void Update() {
        switch (currentState) {
            case PrologueState.needRepairs: {
                var engine = Train.s.carts[0];
                if (!engine.isDestroyed) {
                    EngageStartEngineState();
                }
            }
                break;
            case PrologueState.startEngine: {
                var engine = Train.s.carts[0].GetComponentInChildren<EngineModule>();

                if (engine.GetEffectivePressure() > 1.5f) {
                    DirectControlMaster.s.DisableDirectControl();
                    DirectControlMaster.s.directControlLock = 1f;
                }
                
                if (engine.GetEffectivePressure() >= 1f && !DirectControlMaster.s.directControlInProgress) {
                    EngageGetGunState();
                }
            }
                break;
            case PrologueState.getGun: {
                Matrix4x4 p = originalProjection;
                p.m01 += Mathf.Sin(Time.time * 1.2F) * 0.1F * funkyCamMagnitude;
                p.m10 += Mathf.Sin(Time.time * 1.5F) * 0.1F * funkyCamMagnitude;
                cam.projectionMatrix = p;

                CheckRewardCartLost(cannonState);
                
                if (Train.s.carts.Count >= 3 && !PlayerWorldInteractionController.s.isDragging()) {
                    EngageGetAmmoState();
                }
            }
                break;
            case PrologueState.getAmmoAndKillOneEnemy: {
                CheckRewardCartLost(ammoCartState);


                if (currentValues.enemyHasSpawned) {
                    if (EnemyWavesController.s.GetActiveEnemyCount() <= 0) {
                        EngageTimeToDie();
                    }
                }
            }
                break;
            case PrologueState.timeToDie: {
                if (currentValues.finalEnemiesSpawned) {
                    if (EnemyWavesController.s.GetActiveEnemyCount() <= 10) {
                        for (int i = 0; i < reinforcementsCount; i++) {
                            EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, true);
                            EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, false);
                        }

                        reinforcementsCount += 1;
                    }
                }
            }
                break;
        }
    }

    public int reinforcementsCount = 1;

    void EngageTimeToDie() {
        currentState = PrologueState.timeToDie;
        
        var gunModules = Train.s.GetComponentsInChildren<GunModule>();
        foreach (var gun in gunModules) {
            gun.ActivateGun();
            gun.gunCannotActivateOverride = false;
        }

        StartCoroutine(TimeToDieAnimations());
    }
    
    IEnumerator TimeToDieAnimations() {
        var meteorShard = Train.s.carts[0].GetComponentInChildren<Artifact_MeteorShard>();
        meteorShard.SetParticlesState(true);
        
        yield return new WaitForSeconds(2f);
        
        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, true);
        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, true);
        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, false);
        EnemyWavesController.s.MakeSegmentEnemy(SpeedController.s.currentDistance, new UpgradesController.PathEnemyType(){myType = UpgradesController.PathEnemyType.PathType.regular}, true, false);
        currentValues.finalEnemiesSpawned = true;
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

    private void CheckRewardCartLost(DataSaver.TrainState.CartState rewardCartState) {
        if (currentValues.rewardCartSpawned) {
            if (rewardCart == null) {
                RewardCartLost(rewardCartState);
            }

            if (Vector3.Distance(rewardCart.transform.position, Train.s.trainMiddle.position) > 15) {
                if (rewardCart.GetComponent<IPlayerHoldable>().GetHoldingDrone() == null) {
                    RewardCartLost(rewardCartState);
                }
            }
        }
    }

    void RewardCartLost(DataSaver.TrainState.CartState rewardCartState) {
        if(rewardCart != null)
            Destroy(rewardCart);
        
        WakeUpAnimation.s.Engage();
        CameraController.s.ResetCameraPos();
        
        SpawnNewRewardCart(rewardCartState);

        currentValues.enemyHasSpawned = true;
    }

    public DataSaver.TrainState.CartState cannonState;
    public DataSaver.TrainState.CartState ammoCartState;
    void SpawnNewRewardCart(DataSaver.TrainState.CartState rewardCartState) {
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

        rewardCart = cart.gameObject;
    }

    public float funkyCamMagnitude = 0;
    void EngageGetGunState() {
        currentState = PrologueState.getGun;
        StartCoroutine(GetGunAnimations());
    }
    
    void EngageGetAmmoState() {
        currentState = PrologueState.getAmmoAndKillOneEnemy;
        
        cam.projectionMatrix = originalProjection;
        
        var gunModules = Train.s.GetComponentsInChildren<GunModule>();
        foreach (var gun in gunModules) {
            gun.DeactivateGun();
            gun.gunCannotActivateOverride = true;
        }
        
        Invoke(nameof(SpawnAmmoCartAndFriendlyEnemy),0.5f);
    }

    public GameObject friendlyEnemyBattalion;
    void SpawnAmmoCartAndFriendlyEnemy() {
        SpawnNewRewardCart(ammoCartState);
        rewardCart.GetComponentInChildren<ModuleAmmo>().SetAmmo(0);
        EnemyWavesController.s.SpawnCustomBattalion(friendlyEnemyBattalion, SpeedController.s.currentDistance+30, true, true);

        currentValues.enemyHasSpawned = true;
    }

    public GameObject cartRewardChest;
    IEnumerator GetGunAnimations() {
        funkyCamMagnitude = 0;
        funkyCamMagnitude = 0;

        while (funkyCamMagnitude < 1f) {
            funkyCamMagnitude += 1 * Time.deltaTime;
            if (funkyCamMagnitude > 1f)
                funkyCamMagnitude = 1;
            yield return null;
        }


        var meteorShard = Train.s.carts[0].GetComponentInChildren<Artifact_MeteorShard>();
        meteorShard.SetParticlesState(true);

        var engine = Train.s.carts[0].GetComponentInChildren<EngineModule>();
        engine.currentPressure = 1.2f;
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
        var rewardDistance = SpeedController.s.currentDistance + 30;
        var rewardChestScript = rewardChest.GetComponent<CartRewardOnRoad>();
        rewardChestScript.SetUp(rewardDistance);
        rewardChestScript.OnCustomRewardSpawned.AddListener(RegisterRewardCart);
        
        funkyCamMagnitude = 1;

        while (funkyCamMagnitude > 0f) {
            funkyCamMagnitude -= 0.5f * Time.deltaTime;
            if (funkyCamMagnitude < 0f)
                funkyCamMagnitude = 0;
            yield return null;
        }


        while (SpeedController.s.currentDistance < rewardDistance-2.5f) {
            yield return null;
        }
        
        TimeController.s.SetSlowDownAndPauseState(true);

        yield return new WaitForSecondsRealtime(2f);
        
        TimeController.s.SetSlowDownAndPauseState(false);
    }

    public GameObject rewardCart;
    void RegisterRewardCart(GameObject cart) {
        rewardCart = cart;
        currentValues.rewardCartSpawned = true;
    }

    void EngageStartEngineState() {
        currentState = PrologueState.startEngine;
        
        DirectControlMaster.s.DisableDirectControl();
        var repairModule = Train.s.carts[1].GetComponentInChildren<DroneRepairController>();
        repairModule.droneCannotActivateOverride = false;
        repairModule.ActivateAutoDrone();
        
        RemoveAllHighlights();
        
       Invoke(nameof(HighlightControlEngine), 30f);
    }

    void HighlightControlEngine() {
        currentValues.higlightingControlEngine = true;
        if (currentState != PrologueState.startEngine) {
            return;
        }
        
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


    void RemoveAllHighlights() {
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
        
        PathAndTerrainGenerator.s.SetBiomes(biome, false);
    }


    public void PrologueDone() {
        
    }
}
