using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class InfiniteMapController : MonoBehaviour {
    public static InfiniteMapController s;

    private void Awake() {
        s = this;
    }

    public GameObject mapButton;

    public bool killedAllEnemies = false;
    public bool movedThisTime = false;

    public AnotherMapContainer myMap;
    public Transform dioramaParent;

    public GameObject playerPiece;
    public AnimationCurve playerPieceMoveCurve = AnimationCurve.Linear(0,0,1,1);
    public AnimationCurve parabolaCurve = AnimationCurve.Linear(0,0,1,1);

    public bool nextPathIsEndPath = false;

    private void Start() {
        mapButton.SetActive(false);
        MakeMap();
    }

    void Update()
    {
        if (PlayStateMaster.s.isCombatInProgress()) {
            if (!killedAllEnemies) {
                if (EnemyWavesController.s.GetActiveEnemyCount() <= 0) {
                    killedAllEnemies = true;
                    SectionComplete();
                }
            }
        }

    }

    public void DebugRemakeMap() {
        MakeMap();
        RenderMap();
    }

    [Button]
    public void DebugRedrawMap() {
        RenderMap();
    }

    public int depth = 0;
    void SectionComplete() {
        if(PrologueController.s.isPrologueActive)
            return;
        
        movedThisTime = false;
        depth += 1;
        
        if (nextPathIsEndPath) {
            return;
        }
        
        mapButton.SetActive(true);
        RenderMap();
    }

    public bool isMapOpen = false;
    public void ToggleMap() {
        
        
        isMapOpen = !isMapOpen;

        if (isMapOpen) {
            CameraController.s.EnterMapMode();
        } else {
            CameraController.s.ExitMapMode();
        }
    }
    
    
    public void GotoSection(DioramaHolder holder) {
        if(movedThisTime)
            return;
        movedThisTime = true;

        StartCoroutine(_GoToSection(holder));
    }

    public GameObject transitionScreen;
    public GameObject loadingSliderThing;
    public Slider loadingSlider;
    IEnumerator _GoToSection(DioramaHolder holder) {
        for (int i = 0; i < myMap.allPieces.Count; i++) {
            myMap.allPieces[i].playerIsHere = false;
        }

        holder.playerIsHere = true;
        holder.playerWasHere = true;
        myMap.playerPieceOffset = holder.myOffsetFromRoot;
        
        PruneOtherPaths(holder);

        DecideAndExtendPath(holder);

        PlayerWorldInteractionController.s.canSelect = false;
        GamepadControlsHelper.s.RemovePossibleAction(GamepadControlsHelper.PossibleActions.clickGate);
        
        // move player piece
        var time = 0f;
        var duration = 1f;
        bool transitionStarted = false;
        
        GetComponent<FMODAudioSource>().Play();
        
        while (time < duration) {
            var progress = time / duration;
            playerPiece.transform.position = Vector3.Lerp(dioramaParent.transform.position, holder.myScript.transform.position, playerPieceMoveCurve.Evaluate(progress));
            playerPiece.transform.position += Vector3.up*parabolaCurve.Evaluate(progress)*1.2f;
            time += Time.unscaledDeltaTime;

            if (progress > 0.1f && !transitionStarted) {
                transitionStarted = true;
                transitionScreen.SetActive(true);
                loadingSliderThing.SetActive(false);
                MiniGUI_MapTransitionAnimator.s.Transition(true);
            }
            
            yield return null;
        }
        
        playerPiece.transform.position = holder.myScript.transform.position;
        
        yield return new WaitUntil(() => MiniGUI_MapTransitionAnimator.s.transitionProgress >= 1f);
        
        //loadingBar
        var isBoss = holder.myPiece.myEnemyType.myType == UpgradesController.PathEnemyType.PathType.boss;
        BossController.s.SetBossComing(isBoss);
        
        BiomeController.s.SetBiome(holder.myPiece.dioramaType);
        PathAndTerrainGenerator.s.RemakeLevelTerrain();
        dioramaParent.DeleteAllChildren();

        //loadingSliderThing.SetActive(true);

        var loadingProgress = 0f;
        while (loadingProgress < 1) {
            loadingProgress = PathAndTerrainGenerator.s.terrainGenerationProgress;
            loadingProgress = Mathf.Clamp01(loadingProgress);
            loadingSlider.value = loadingProgress;
            yield return null;
        }

        loadingSlider.value = 1f;

        //RepairAndReloadTrain();

        //loadingSliderThing.SetActive(false);
        NextSectionStarted(holder);

        GetComponent<FMODAudioSource>().Stop();
        
        MiniGUI_MapTransitionAnimator.s.Transition(false);
        yield return new WaitUntil(() => MiniGUI_MapTransitionAnimator.s.transitionProgress >= 1f);
        
        
        transitionScreen.SetActive(false);
        
        
        PlayerWorldInteractionController.s.canSelect = true;
    }

    private static void RepairAndReloadTrain() {
        for (int i = 0; i < Train.s.carts.Count; i++) {
            var cart = Train.s.carts[i];
            cart.FullyRepair();
        }

        var combatModules = Train.s.GetComponentsInChildren<IActiveDuringCombat>();

        for (int i = 0; i < combatModules.Length; i++) {
            combatModules[i].ActivateForCombat();
            if (combatModules[i] is ModuleAmmo ammo) {
                ammo.Reload(-1, false);
            }
        }
    }

    void PruneOtherPaths(DioramaHolder holder) {
        int n = 0;
        while (holder.myParent != null) {
            holder.myParent.connectedPieces.Clear();
            holder.myParent.connectedPieces.Add(holder);
            holder = holder.myParent;
            n++;
            if (n > 1000) {
                throw new Exception("infinite loop!");
            }
        }
    }

    void NextSectionStarted(DioramaHolder holder) {
        // spawn reward
        var spawnDist = SpeedController.s.currentDistance + 10f;
        switch (holder.myPiece.myRewardType) {
            case RewardTypes.cart:
                StopAndPick3RewardUIController.s.SpawnCartRewardAtDistance(spawnDist, 
                    new DataSaver.TrainState.CartState(){uniqueName = holder.myPiece.rewardUniqueName}
                    );
                break;
            case RewardTypes.gem:
                StopAndPick3RewardUIController.s.SpawnMiniGemRewardAtDistance(spawnDist, 
                    new DataSaver.TrainState.ArtifactState(){uniqueName = holder.myPiece.rewardUniqueName}
                );
                break;
            case RewardTypes.bigGem:
                StopAndPick3RewardUIController.s.SpawnBigGemRewardAtDistance(spawnDist, 
                    new DataSaver.TrainState.ArtifactState(){uniqueName = holder.myPiece.rewardUniqueName}
                );
                break;
            case RewardTypes.randomCart:
                StopAndPick3RewardUIController.s.SpawnCartRewardAtDistance(spawnDist);
                break;
            case RewardTypes.randomGem:
                StopAndPick3RewardUIController.s.SpawnMiniGemRewardAtDistance(spawnDist);
                break;
            case RewardTypes.randomBigGem:
                StopAndPick3RewardUIController.s.SpawnBigGemRewardAtDistance(spawnDist);
                break;
        }


        // spawn enemies
        var enemyType = holder.myPiece.myEnemyType;
        
        EnemyWavesController.s.SpawnEnemiesOnSegment(SpeedController.s.currentDistance + 50, 0, enemyType);

        // close map
        killedAllEnemies = false;
        isMapOpen = true;
        ToggleMap();
        mapButton.SetActive(false);
    }

    void MakeMap() {
        myMap = new AnotherMapContainer();
        myMap.rootPiece = new DioramaHolder(){playerIsHere = true, playerWasHere = true};
        myMap.allPieces.Add(myMap.rootPiece);

        MakeSplit(myMap.rootPiece);
    }


    void DecideAndExtendPath(DioramaHolder holder) {
        if (holder.connectedPieces.Count == 0) {
            if (holder.myDepth < 6) {
                MakeSplit(holder);
            } else if (holder.myPiece.myEnemyType.myType == UpgradesController.PathEnemyType.PathType.boss) {
                nextPathIsEndPath = true;
            } else{
                MakeBossPath(holder);
            }
        }
    }

    private static void MakeBossPath(DioramaHolder holder) {
        var root = holder;
        var offsetFromCurrentPos = dioramaOffsetTouching + 0.3f;
        var nextLayerTopPieceOffset = new Vector3(0, 0, offsetFromCurrentPos) + root.myOffsetFromRoot;

        var piece = new DioramaHolder() { myOffsetFromRoot = nextLayerTopPieceOffset };
        piece.myDepth = root.myDepth + 1;
        root.connectedPieces.Add(piece);
        piece.myParent = root;

        piece.myPiece.myEnemyType = new UpgradesController.PathEnemyType() { myType = UpgradesController.PathEnemyType.PathType.boss };
        piece.myPiece.myRewardType = RewardTypes.randomBigGem;
    }

    void MakeSplit(DioramaHolder root) {
        var nextLayerCount = 3;
        var topDownOffset = dioramaOffsetTouching + 0.4f;
        var offsetFromCurrentPos = dioramaOffsetTouching + 0.3f;
        var nextLayerTopPieceOffset = new Vector3(-((nextLayerCount-1) / 2f * topDownOffset), 0, offsetFromCurrentPos) + root.myOffsetFromRoot;
        
        for (int i = 0; i < nextLayerCount; i++) {
            MakeSection(root, nextLayerTopPieceOffset + (Vector3.right * i * topDownOffset));
        }
    }
    
    const float dioramaOffsetTouching = 1.6f;

    DioramaHolder MakeSection(DioramaHolder root, Vector3 offset) {
        bool isFirstSection = (root.myDepth < 3);
        
        
        var rewards = MakeRewardSection(isFirstSection);
        
        var pos = offset;
        var piece = MakePieceWithReward(pos, root, rewards[0]);
        rewards.RemoveAt(0);
        root.connectedPieces.Add(piece);
        piece.myParent = root;

        while (rewards.Count > 0) {
            pos += Vector3.forward*1.6f;
            var nextPiece = MakePieceWithReward(pos, piece, rewards[0]);
            rewards.RemoveAt(0);
            piece.connectedPieces.Add(nextPiece);
            nextPiece.myParent = piece;
            piece = nextPiece;
        }

        return piece;
    }

    List<RewardTypes> MakeRewardSection(bool firstSection) {
        List<RewardTypes> rewards = new List<RewardTypes>();
        
        rewards.Add(Random.value < 0.6f ? RewardTypes.gem : RewardTypes.randomGem);
        rewards.Add(Random.value < 0.6f ? RewardTypes.gem : RewardTypes.randomGem);
        rewards.Add(Random.value < 0.6f ? RewardTypes.bigGem : RewardTypes.randomBigGem);
        if (firstSection) {
            rewards.Add(Random.value < 0.6f ? RewardTypes.gem : RewardTypes.randomGem);
        } else {
            rewards.Add(Random.value < 0.6f ? RewardTypes.cart : RewardTypes.randomCart);
        }
        
        rewards = ExtensionMethods.Shuffle(rewards);

        return rewards;
    }

    DioramaHolder MakePieceWithReward(Vector3 offset, DioramaHolder prevPiece, RewardTypes reward) {
        var piece = new DioramaHolder() { myOffsetFromRoot = offset };
        piece.myDepth = prevPiece.myDepth+1;
        piece.myPiece.myRewardType = reward;

        switch (piece.myPiece.myRewardType) {
            case RewardTypes.cart:
                piece.myPiece.rewardUniqueName = UpgradesController.s.GetCartReward();
                break;
            case RewardTypes.gem:
                piece.myPiece.rewardUniqueName = UpgradesController.s.GetGemReward(false);
                break;
            case RewardTypes.bigGem:
                piece.myPiece.rewardUniqueName = UpgradesController.s.GetGemReward(true);
                break;
        }

        if (IsRewardTypeElite(piece.myPiece.myRewardType)) {
            piece.myPiece.myEnemyType = new UpgradesController.PathEnemyType() { myType = UpgradesController.PathEnemyType.PathType.elite };
        } else {
            piece.myPiece.myEnemyType = new UpgradesController.PathEnemyType() { myType = UpgradesController.PathEnemyType.PathType.regular };
            
            if (piece.myDepth < 3) {
                piece.myPiece.myEnemyType.myType = UpgradesController.PathEnemyType.PathType.easy;
            }
        }
        
        piece.myPiece.dioramaType = BiomeController.s.GetRandomBiomeVariantForCurrentAct();

        myMap.allPieces.Add(piece);
        return piece;
    }

    bool IsRewardTypeElite(RewardTypes checkType) {
        return IsRewardTypeCart(checkType) || IsRewardTypeBigGem(checkType);
    }
    
    bool IsRewardTypeCart(RewardTypes checkType) {
        return checkType == RewardTypes.cart || checkType == RewardTypes.randomCart;
    }
    
    bool IsRewardTypeBigGem(RewardTypes checkType) {
        return checkType == RewardTypes.bigGem || checkType == RewardTypes.randomBigGem;
    }


    void RenderMap() {
        dioramaParent.DeleteAllChildren();

        List<DioramaHolder> piecesToProcess = new List<DioramaHolder>();
        piecesToProcess.Add(myMap.rootPiece);
        var baseOffset = (-myMap.playerPieceOffset*2) + dioramaParent.position;

        for (int i = 0; i < myMap.allPieces.Count; i++) {
            myMap.allPieces[i].canGoHere = false;
        }

        int n = 0;
        while (piecesToProcess.Count > 0) {
            var piece = piecesToProcess[0];
            RenderPiece(baseOffset, piece);

            for (int i = 0; i < piece.connectedPieces.Count; i++) {
                var nextPiece = piece.connectedPieces[i];
                piecesToProcess.Add(nextPiece);
                if (piece.playerIsHere) {
                    nextPiece.canGoHere = true;
                }
            }
            
            piecesToProcess.RemoveAt(0);
            n++;
            if (n > 1000) {
                throw new Exception("loop in map!");
            }
        }

        playerPiece.transform.position = dioramaParent.transform.position;
    }

    public GameObject randomCart;
    public GameObject randomGem;
    public GameObject randomBigGem;
    void RenderPiece(Vector3 baseOffset, DioramaHolder piece) {
        var prefab = BiomeController.s.GetBiomeVariant(piece.myPiece.dioramaType).GetDioramaPrefab();
        var script = Instantiate(prefab,  baseOffset + myMap.playerPieceOffset + piece.myOffsetFromRoot, Quaternion.identity, dioramaParent).GetComponent<DioramaScript>();
        script.Initialize(piece);

        if (!piece.playerWasHere) {
            var gemOffset = new Vector3(0, 0.604f, 0f);
            switch (piece.myPiece.myRewardType) {
                case RewardTypes.cart:
                    var cart = Train.InstantiateCartFromState(new DataSaver.TrainState.CartState() { uniqueName = piece.myPiece.rewardUniqueName },
                        script.transform.position, script.transform.rotation);
                    cart.transform.SetParent(script.transform);
                    break;
                case RewardTypes.gem:
                case RewardTypes.bigGem:
                    var gem = Train.InstantiateArtifactFromState(new DataSaver.TrainState.ArtifactState() { uniqueName = piece.myPiece.rewardUniqueName },
                        script.transform.position + gemOffset, script.transform.rotation);
                    gem.transform.SetParent(script.transform);
                    break;
                case RewardTypes.randomCart:
                    Instantiate(randomCart, script.transform.position, script.transform.rotation, script.transform);
                    break;
                case RewardTypes.randomGem:
                    Instantiate(randomGem, script.transform.position + gemOffset, script.transform.rotation, script.transform);
                    break;
                case RewardTypes.randomBigGem:
                    Instantiate(randomBigGem, script.transform.position + gemOffset, script.transform.rotation, script.transform);
                    break;
            }
        }
    }

    [Serializable]
    public class AnotherMapContainer {
        public DioramaHolder rootPiece;
        public Vector3 playerPieceOffset;
        
        public List<DioramaHolder> allPieces = new List<DioramaHolder>();
    }

    [Serializable]
    public class DioramaHolder {
        public DioramaHolder myParent;
        public bool playerIsHere = false;
        public bool playerWasHere = false;
        public bool canGoHere = false;
        public int myDepth = 0;
        public DioramaPiece myPiece = new DioramaPiece();
        public Vector3 myOffsetFromRoot;
        public DioramaScript myScript;
        public List<DioramaHolder> connectedPieces = new List<DioramaHolder>();
    }
    
    [Serializable]
    public class DioramaPiece {
        public int dioramaType = 0;
        public UpgradesController.PathEnemyType myEnemyType;
        public RewardTypes myRewardType;
        public string rewardUniqueName;
    }

    public enum RewardTypes {
        nothing=0, cart=1, gem=2, bigGem=3, randomCart=4, randomGem=5, randomBigGem=6
    }

    public RewardTypesWithWeights[] myRewards = {
        //new RewardTypesWithWeights(RewardTypes.cart, 1),
        new RewardTypesWithWeights(RewardTypes.gem, 2),
        new RewardTypesWithWeights(RewardTypes.bigGem, 1),
        //new RewardTypesWithWeights(RewardTypes.randomCart, 2),
        new RewardTypesWithWeights(RewardTypes.randomGem, 4),
        new RewardTypesWithWeights(RewardTypes.randomBigGem, 2),
    };
     
    [System.Serializable]
    public class RewardTypesWithWeights {
        [HorizontalGroup(LabelWidth = 50)]
        public RewardTypes type;
        [HorizontalGroup(LabelWidth = 20, Width = 100)]
        public float weight = 1f;


        public RewardTypesWithWeights(){}
	
        public RewardTypesWithWeights(RewardTypes _type, float _weight) {
            type = _type;
            weight = _weight;
        }
	
        public static RewardTypes WeightedRandomRoll(RewardTypesWithWeights[] F) {
            var totalFreq = 0f;
            for (int i = 0; i < F.Length; i++) {
                totalFreq += F[i].weight;
            }
		
            var roll = Random.Range(0,totalFreq);
            // Ex: we roll 0.68
            //   #0 subtracts 0.25, leaving 0.43
            //   #1 subtracts 0.4, leaving 0.03
            //   #2 is a $$anonymous$$t
            var index = -1;
            for(int i=0; i<F.Length; i++) {
                if (roll <= F[i].weight) {
                    index=i; break;
                }
                roll -= F[i].weight;
            }
            // just in case we manage to roll 0.0001 past the $$anonymous$$ghest:
            if(index==-1) 
                index=F.Length-1;

            return F[index].type;
        }
    }
    
}
