using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class PathAndTerrainGenerator : MonoBehaviour {
    public static PathAndTerrainGenerator s;

    private void Awake() {
        s = this;
        for (int i = myTerrains.Count-1; i >=0; i--) {
            Destroy(myTerrains[i].terrain.gameObject);
        }

        ClearAllMyPaths();
        myTerrains.Clear();
        currentTerrainGenCount = 0;
    }

    private void Start() {
        SetBiomes();
    }

    public List<PathGenerator.TrainPath> myPaths = new List<PathGenerator.TrainPath>();
    public List<TerrainGenerator.TrainTerrain> myTerrains = new List<TerrainGenerator.TrainTerrain>();
    public Dictionary<int, List<GameObject>> myTracks = new Dictionary<int, List<GameObject>>();
    public List<PathGenerator.TrainPath> cityStampPaths = new List<PathGenerator.TrainPath>();
    List<PathGenerator.TrainPath> _comboList = new List<PathGenerator.TrainPath>();
    List<PathGenerator.TrainPath> comboList {
        get { return GetComboList(); }
    }

    public List<PathGenerator.TrainPath> GetComboList() {
        if (isComboListDirty) {
            isComboListDirty = false;
            _comboList = new List<PathGenerator.TrainPath>(myPaths.Count + cityStampPaths.Count);
            _comboList.AddRange(myPaths);
            _comboList.AddRange(cityStampPaths);
        }

        return _comboList;
    }
    public bool isComboListDirty = false;

    public GameObject distantMountains;
    
    private int lastId = 0;
    void AddToMyPaths(PathGenerator.TrainPath toAdd) {
        if (toAdd.trackObjectsId <= 0) {
            lastId += 1;
            toAdd.trackObjectsId = lastId;
        }
        myPaths.Add(toAdd);
    }

    void RemoveFromMyPaths(PathGenerator.TrainPath toRemove) {
        myPaths.Remove(toRemove);
        if (myTracks.ContainsKey(toRemove.trackObjectsId)) {
            var listOfTracks = myTracks[toRemove.trackObjectsId];
            for (int i = 0; i < listOfTracks.Count; i++) {
                listOfTracks[i].GetComponent<PooledObject>().DestroyPooledObject();
            }
        }
        myTracks.Remove(toRemove.trackObjectsId);
    }

    void ClearAllMyPaths() {
        lastId = 0;
        myPaths.Clear();
        foreach (var keyValuePair in myTracks) {
            for (int i = 0; i < keyValuePair.Value.Count; i++) {
                keyValuePair.Value[i].GetComponent<PooledObject>().DestroyPooledObject();
            }
        }
        myTracks.Clear();
    }

    public Biome[] biomes;
	
    [System.Serializable]
    public class Biome {
        public GameObject terrainPrefab;
        public Light sun;
        public SkyboxParametersScriptable skybox;
        public float skyboxLightIntensity = 1.56f;
    }

    public int biomeOverride = -1;

    public ObjectPool terrainPool;

    public float trackDistance;

    public PathTree currentPathTree;
    public float currentPathTreeOffset = 0;
    
    [Serializable]
    public class PathTree {
        [ShowInInspector]
        public PathTree prevPathTree;
        public PathGenerator.TrainPath myPath;
        [ShowInInspector]
        public PathTree leftPathTree;
        [ShowInInspector]
        public PathTree rightPathTree;

        public bool startPath = false;
        public bool endPath = false;

        public int myDepth = 0;

        public PathGenerator.TrainPath cityStampPath;
        
        public PathType myType = PathType.regular;
        public UpgradesController.PathEnemyType enemyType;
        public enum PathType{ regular = 0, end = 1, infinite = 2}
        
        public bool debugDrawGizmo = false;
    }


    public UnityEvent OnNewTerrainStabilized = new UnityEvent();
    public bool initialTerrainMade = false;


    public void SetBiomes() {
        SetBiomes(-1);
    }

    private int currentBiomeIndex;
    public void SetBiomes(int _biomeOverride, bool repopulateTerrain = true) {
        Biome currentBiome;
        var actualBiomeOverride = -1;
        if (biomeOverride >= 0) {
            actualBiomeOverride = biomeOverride;
        }
        if (_biomeOverride >= 0) {
            actualBiomeOverride = _biomeOverride;
        }
        if (actualBiomeOverride < 0) {
            var targetBiome = DataSaver.s.GetCurrentSave().currentRun.currentAct-1;
            if (targetBiome < 0 || targetBiome > biomes.Length) {
                Debug.LogError($"Illegal biome {targetBiome}");
                targetBiome = 0;
            }

            Debug.Log($"Biome set {targetBiome}");
            actualBiomeOverride = targetBiome;
        }

        currentBiomeIndex = actualBiomeOverride;
        currentBiome = biomes[currentBiomeIndex];

        ApplyBiome(currentBiome);

        if(repopulateTerrain)
            terrainPool.RePopulateWithNewObject(currentBiome.terrainPrefab);
    }


    /*public void MakeFakePathForMissionRewards() {
        terrainGenerationProgress = 0;
        needReflectionProbe = true;
        ClearTerrains();
        ClearAllMyPaths();
        cityStampPaths.Clear();
        terrainViewRange = 20;

        var stopDistance = 7.5f;

        var segmentDistance = 5f;
        var endStationPath = _pathGenerator.MakeStationAtBeginningPath(-Vector3.forward * PathGenerator.stationStraightDistance / 2f, Vector3.forward,Vector3.forward, segmentDistance);
        AddToMyPaths(endStationPath);
        isComboListDirty = true;
        
        var cityDistance =   stopDistance + PathGenerator.stationStraightDistance/2f + endStationCenterOffset;
        var cityStampPath = _pathGenerator.MakeCityStampPath(PathGenerator.GetPointOnLine(endStationPath, cityDistance), PathGenerator.GetDirectionOnTheLine(endStationPath, cityDistance));
        cityStampPaths.Add(cityStampPath);
        isComboListDirty = true;
        
        currentPathTree = new PathTree() {
            startPath = true,
            myPath = endStationPath,
            cityStampPath = cityStampPath
        };

        initialTerrainMade = false;

        currentPathTreeOffset = -PathGenerator.stationStraightDistance / 2f;
        generatedPathDepth = 0;

        var trainStationEnd = PathSelectorController.s.trainStationEnd;
        trainStationEnd.GetComponent<TrainStation>().stationDistance = stopDistance;
        trainStationEnd.GetComponent<TrainStation>().Update();
        trainStationEnd.SetActive(true);
        
        PathSelectorController.s.trainStationStart.SetActive(false);
        

        SpeedController.s.missionDistance = 0;
        SpeedController.s.currentDistance = 0;

        StartCoroutine(ReDrawTerrainAroundCenter(true));
    }*/


    [Button]
    public void DebugMakeCityStamp(Vector3 offset, Quaternion direction) {
        cityStampPaths.Clear();
        var cityPath = _pathGenerator.MakeCityStampPath(offset, direction);
        cityStampPaths.Add(cityPath);
        isComboListDirty = true;
    }

    [Button]
    public void DebugMakeCityPath() {
        ClearAllMyPaths();
        var starterStationPath = _pathGenerator.MakeStationAtBeginningPath(-Vector3.forward * PathGenerator.stationStraightDistance / 2f, Vector3.forward, Vector3.forward, Random.Range(30,50));
        AddToMyPaths(starterStationPath);
        isComboListDirty = true;
    }


    public float terrainGenerationProgress = 0;
    private bool needReflectionProbe = false;

    private float regularTerrainViewRange = 75;
    private float mapTerrainViewRange = 20;
    public void MakeStarterAreaTerrain() {
        terrainGenerationProgress = 0;
        needReflectionProbe = true;
        ClearTerrains();
        ClearAllMyPaths();
        cityStampPaths.Clear();
        terrainViewRange = mapTerrainViewRange;
        
        var starterStationPath = _pathGenerator.MakeStationAtBeginningPath(-Vector3.forward * PathGenerator.stationStraightDistance / 2f, Vector3.forward, Vector3.forward, Random.Range(30,50));
        AddToMyPaths(starterStationPath);
        isComboListDirty = true;
        
        var cityDistance =  PathGenerator.stationStraightDistance/2f;
        var cityStampPath = _pathGenerator.MakeCityStampPath(PathGenerator.GetPointOnLine(starterStationPath, cityDistance), PathGenerator.GetDirectionOnTheLine(starterStationPath, cityDistance));
        cityStampPaths.Add(cityStampPath);
        isComboListDirty = true;
        
        currentPathTree = new PathTree() {
            startPath = true,
            myPath = starterStationPath,
            cityStampPath = cityStampPath
        };

        initialTerrainMade = false;

        currentPathTreeOffset = -PathGenerator.stationStraightDistance / 2f;
        
        StartCoroutine(ReDrawTerrainAroundCenter(true));
    }

    public static Vector3 CircleTerrainCenter() {
        return Vector3.left * 100;
    }
    public void MakeCircleTerrainForPrologue() {
        terrainGenerationProgress = 0;
        needReflectionProbe = true;
        ClearTerrains();
        ClearAllMyPaths();
        cityStampPaths.Clear();
        terrainViewRange = regularTerrainViewRange;
        
        var isGoodTrack = false;

        PathGenerator.TrainPath track = null;
        var n = 0;
        while (!isGoodTrack) {
            track = _pathGenerator.MakeCirclePath(CircleTerrainCenter());
            isGoodTrack = Vector3.Distance(track.points[0], track.points[^1]) < 0.1f && track.endRotateStartPoint != 10000;
            n++;

            if (n > 20) {
                Debug.LogError("Could not make a good track in under 20 tries");
                break;
            }
        }
        Debug.Log($"made good path in {n} tries");
        AddToMyPaths(track);
        isComboListDirty = true;
        
        currentPathTree = new PathTree() {
            startPath = true,
            myPath = track,
        };
        currentPathTree.leftPathTree = currentPathTree;
        currentPathTree.rightPathTree = currentPathTree;

        initialTerrainMade = false;

        currentPathTreeOffset = 35;
        
        distantMountains.SetActive(false);
        
        StartCoroutine(ReDrawTerrainAroundCenter(true));
    }


    [Button]
    public void DebugMakeCirclePath() {
        ClearAllMyPaths();
        var isGoodTrack = false;

        PathGenerator.TrainPath track = null;
        var n = 0;
        while (!isGoodTrack) {
            track = _pathGenerator.MakeCirclePath(CircleTerrainCenter());
            isGoodTrack = Vector3.Distance(track.points[0], track.points[^1]) < 1;
            n++;

            if (n > 20) {
                Debug.LogError("Could not make a good track in under 20 tries");
                break;
            }
        }
        Debug.Log($"made good path in {n} tries");
        AddToMyPaths(track);
        isComboListDirty = true;
    }

    [Button]
    public void DebugMakeTerrain(int viewCount = 3) {
        myTerrains.Clear();
        //var viewCount = Mathf.CeilToInt(20 * 2f / TerrainGenerator.terrainWidth);
        if (viewCount % 2 == 0) {
            viewCount += 1;
        }
        var terrainCount = (int)Mathf.Pow( viewCount,2);

        for (int i = 0; i < terrainCount; i++) {
            var offset = GetSpiralNumber(i);
            var coordinates = new Vector2Int(offset.x, offset.y);
            GetComponent<TerrainGenerator>().MakeTerrainDistanceMaps(
                coordinates,
                /*center,*/
                comboList,
                TerrainGenDone
            );
        }
    }

    [Button]
    public void DebugMakeTerrainPostProcess() {
        for (int i = 0; i < myTerrains.Count; i++) {
            GetComponent<TerrainGenerator>().ReprocessTerrainChanges(myTerrains[i]);
        }
    }
    
    [Button]
    public void DebugMakeTracks() {
        /*var viewBound = new Bounds(Vector3.zero, Vector3.one * terrainViewRange);

        /*for (int i =  myTracks.Count-1; i >=0; i--) {
            myTracks[i].GetComponent<PooledObject>().DestroyPooledObject();
        }
        myTracks.Clear();#1#

        var deleteList = new List<Vector3>();
        foreach (var keyValuePair in myTracks) {
            if (!viewBound.Contains(keyValuePair.Key+center)) {
                keyValuePair.Value.GetComponent<PooledObject>().DestroyPooledObject();
                deleteList.Add(keyValuePair.Key);
            }
        }

        foreach (var key in deleteList) {
            myTracks.Remove(key);
        }

        for (int i = 0; i < myPaths.Count; i++) {
            if (myPaths[i].bounds.Intersects(viewBound)) {
                
                var trainPath = myPaths[i];
                var distance = 0f;
                while (distance < trainPath.length) {
                    var point = PathGenerator.GetPointOnLine(trainPath, distance);
                    if (viewBound.Contains(point) && !myTracks.ContainsKey(point - center)) {
                        var newTrack = trackPool.Spawn(point, PathGenerator.GetDirectionOnTheLine(trainPath, distance));
                        myTracks[point-center] = newTrack;
                    }
                    distance += trackDistance;
                }
            }
        }*/
    }

    
    private ConstructedLevel activeLevel => PlayStateMaster.s.currentLevel;
    public void MakeLevelTerrain() {
        if (activeLevel == null) {
            return;
        }
        terrainViewRange = regularTerrainViewRange;
        
        PruneAndExtendPaths();
        StartCoroutine(ReDrawTerrainAroundCenter());
    }

    public void PruneAndExtendPaths() {
        var pathGenerateDistance = 300;
        
        // extend
        ForkPath(currentPathTree, 0, pathGenerateDistance);

        // prune
        PrunePath(currentPathTree, 0, pathGenerateDistance,0,3, new List<PathTree>());
    }


    void PrunePath(PathTree pathTree, float curDistance, float maxDistance, float curDepth, float minDepth, List<PathTree> processedList) {
        if(pathTree == null)
            return;
        if(processedList.Contains(pathTree))
        {
            return;
        }

        if (curDistance > maxDistance && curDepth >= minDepth) {
            RemoveFromMyPaths(pathTree.myPath);
            isComboListDirty = true;
            if (pathTree.leftPathTree != null) {
                pathTree.leftPathTree.prevPathTree = null;
            }
            if (pathTree.rightPathTree != null) {
                pathTree.rightPathTree.prevPathTree = null;
            }
            if (pathTree.prevPathTree != null) {
                var prevPath = pathTree.prevPathTree;
                if (prevPath.leftPathTree == pathTree) {
                    prevPath.leftPathTree = null;
                }else if (prevPath.rightPathTree == pathTree) {
                    prevPath.rightPathTree = null;
                }
            }

            cityStampPaths.Remove(pathTree.cityStampPath);
            isComboListDirty = true;
        }
        
        processedList.Add(pathTree);
        
        curDistance += pathTree.myPath.length;
        curDepth += 1;

        PrunePath(pathTree.prevPathTree, curDistance, maxDistance, curDepth, minDepth,processedList);
        PrunePath(pathTree.leftPathTree, curDistance, maxDistance, curDepth, minDepth,processedList);
        PrunePath(pathTree.rightPathTree, curDistance, maxDistance, curDepth, minDepth,processedList);
    }


    /*private int minCastleDepth = 2;
    private float endStationChance = 0.25f;
    private bool forceEndStation = false;*/
    void ForkPath(PathTree pathTree, float curDistance, float maxDistance) {
        if(pathTree.myType == PathTree.PathType.end)
            return;

        var newPathDepth = pathTree.myDepth + 1;
        
        var startPoint = pathTree.myPath.points[^1];
        var startDirection = PathGenerator.GetDirectionVectorOnTheLine(pathTree.myPath, pathTree.myPath.length);

        var pathType = PathTree.PathType.regular;
        /*if (makeInfiniteBossPath) {
            pathType = PathGenerator.TrainPath.PathType.infinite;
        }*/

        var leftEnemy = MapController.s.GetMapEnemyType(true, newPathDepth);
        var rightEnemy = MapController.s.GetMapEnemyType(false, newPathDepth);

        var leftSegmentDistance = MapController.s.GetSegmentDistance(true, newPathDepth);
        var rightSegmentDistance = MapController.s.GetSegmentDistance(false, newPathDepth);

        switch (pathTree.myType) {
            case PathTree.PathType.regular:
            {
                // left fork
                if (pathTree.leftPathTree == null) {
                    var leftSegmentDirection = Quaternion.Euler(0, -30 + Random.Range(-15, 15), 0) * startDirection;
                    pathTree.leftPathTree = _MakeForkedPath(pathTree, curDistance, maxDistance, pathType, startPoint, startDirection, leftSegmentDirection, leftSegmentDistance,
                        leftEnemy, newPathDepth);
                }

                //right fork
                if (pathTree.rightPathTree == null) {
                    var rightSegmentDirection = Quaternion.Euler(0, 30 + Random.Range(-15, 15), 0) * startDirection;
                    pathTree.rightPathTree = _MakeForkedPath(pathTree, curDistance, maxDistance, pathType, startPoint, startDirection, rightSegmentDirection, rightSegmentDistance,
                        rightEnemy, newPathDepth);
                }
            }
                break;
            /*case PathGenerator.TrainPath.PathType.infinite: {
                // left fork - infinite path doesn't fork so there is only the left path
                if (pathTree.leftPathTree == null) {
                    generatedPathDepth += 1;
                    var leftSegmentDirection = startDirection;
                    pathTree.leftPathTree = _MakeForkedPath(pathTree, curDistance, maxDistance, pathType, startPoint, startDirection, leftSegmentDirection, segmentDistance);
                    pathTree.leftPathTree.myPath.pathRewardUniqueName = "";
                }
            }
                break;*/
        }
    }

    private float endStationCenterOffset = 14;
    private PathTree _MakeForkedPath(PathTree pathTree, float curDistance, float maxDistance, PathTree.PathType pathType, Vector3 startPoint, Vector3 startDirection, Vector3 segmentDirection, float segmentDistance, 
        UpgradesController.PathEnemyType enemyType, int depth) {
        switch (pathType) {
            case PathTree.PathType.end: {
                var newStationEndPath = _pathGenerator.MakeStationAtEndPath(startPoint, startDirection, segmentDirection, segmentDistance, true);
                var cityDistance = segmentDistance + PathGenerator.stationStraightDistance/2f + endStationCenterOffset;
                var cityStampPath = _pathGenerator.MakeCityStampPath(PathGenerator.GetPointOnLine(newStationEndPath, cityDistance), PathGenerator.GetDirectionOnTheLine(newStationEndPath, cityDistance));
                AddToMyPaths(newStationEndPath);
                isComboListDirty = true;
                var newStationEndPathTree = new PathTree() {
                    prevPathTree = pathTree,
                    myPath = newStationEndPath,
                    endPath = true,
                    cityStampPath = cityStampPath,
                    enemyType = enemyType,
                    myDepth = depth,
                };
                cityStampPaths.Add(cityStampPath);
                isComboListDirty = true;
                return newStationEndPathTree;
            }
            case PathTree.PathType.regular: {
                var newPath = _pathGenerator.MakeTrainPath(startPoint, startDirection, segmentDirection, segmentDistance, true);
                AddToMyPaths(newPath);
                isComboListDirty = true;
                var newPathTree = new PathTree() {
                    prevPathTree = pathTree,
                    myPath = newPath,
                    enemyType = enemyType,
                    myDepth = depth,
                };

                curDistance += newPath.length;

                if (curDistance < maxDistance) {
                    ForkPath(newPathTree, curDistance, maxDistance);
                }
            
                return newPathTree;
            }
            /*case PathGenerator.TrainPath.PathType.infinite: {
                var newPath = _pathGenerator.MakeTrainPath(startPoint, startDirection, segmentDirection, segmentDistance, true);
                AddToMyPaths(newPath);
                isComboListDirty = true;
                var newPathTree = new PathTree() {
                    prevPathTree = pathTree,
                    myPath = newPath
                };

                curDistance += newPath.length;

                if (curDistance < maxDistance) {
                    ForkPath(newPathTree, curDistance, maxDistance);
                }
            
                return newPathTree;
            }*/
            default:
                throw new Exception($"Unsupported train path type {pathType}");
        }
    }
    
    


    /*[Button]
    public void MakePath() {
        myPaths.Add(GetComponent<PathGenerator>().MakeTrainPath(transform.position-Vector3.forward*100,Vector3.forward, 200));
    }*/

    public Vector3 center = Vector3.zero;
    public Vector3 drawCenter = Vector3.zero;
    private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();

    private float terrainWidth => TerrainGenerator.terrainWidth;
    public int currentTerrainGenCount = 0;
    private int maxThreads => SystemInfo.processorCount-1;
    /*public void MakeTerrain() {
        print($"--------------------- Max Threads: {maxThreads}");
        for (int i = 0; i < myTerrains.Count; i++) {
            if (myTerrains[myTerrains.Count - 1 - i] != null && myTerrains[myTerrains.Count - 1 - i].terrain != null) {
                myTerrains[myTerrains.Count - 1 - i].terrain.GetComponent<PooledObject>().DestroyPooledObject();
                
            }
        }

        myTerrains.Clear();
        /*_stopwatch.Reset();
        _stopwatch.Start();#1#
        if (Application.isPlaying) {
            StartCoroutine(ReDrawTerrainAroundCenter());
        } else {
            for (int x = -2; x <= 2; x++) {
                for (int y = -2; y <= 2; y++) {
                    GetComponent<TerrainGenerator>().MakeTerrainDistanceMaps(
                        new Vector2Int(x,y), 
                        center,
                        myPaths,
                        TerrainGenDone
                        );
                }
            }
        }
    }*/

    [Button]
    void DebugReDrawTerrainAroundCenter() {
        if (Application.isPlaying) {
            StartCoroutine(ReDrawTerrainAroundCenter());
        }
    }

    public float terrainViewRange = 10;
    public bool reDrawing = false;
    IEnumerator ReDrawTerrainAroundCenter(bool instant = false) {
        while (reDrawing) {
            yield return null;
        }

        //Debug.Break();
        reDrawing = true;
        
        for (int i = 0; i < myTerrains.Count; i++) {
            myTerrains[i].needToBePurged = true;
        }

        if (!makingTracks) {
            StartCoroutine(MakeTracksAroundCenter(instant));
        }

        var diff =  drawCenter;
        var viewCount = Mathf.CeilToInt(terrainViewRange * 2 / TerrainGenerator.terrainWidth);
        if (viewCount % 2 == 0) {
            viewCount += 1;
        }
        var terrainCount = (int)Mathf.Pow( viewCount,2);
        var addition = 1f / terrainCount;
        terrainGenerationProgress -= 0.1f;
        
        for (int i = 0; i < terrainCount; i++) {
            var offset = GetSpiralNumber(i);
            if (offset.magnitude > viewCount/2f) {
                continue;
            }
            offset.x += Mathf.RoundToInt(diff.x / terrainWidth);
            offset.y += Mathf.RoundToInt(diff.z / terrainWidth);
            var coordinates = new Vector2Int(offset.x, offset.y);

            var existingTerrain = GetTerrainAtCoordinates(coordinates);
            //Debug.Log($"Trying to draw terrain at coords {coordinates.x}, {coordinates.y} with diff {diff/terrainWidth}");
            if (existingTerrain == null) {
                
                GetComponent<TerrainGenerator>().MakeTerrainDistanceMaps(
                    coordinates,
                    /*center,*/
                    comboList,
                    TerrainGenDone
                );
                currentTerrainGenCount += 1;
                if (!instant) {
                    yield return null;
                    yield return null;
                }

            } else {
                existingTerrain.needToBePurged = false;
            }

            terrainGenerationProgress += addition;
            /*while (currentTerrainGenCount > maxThreads) {
                yield return null;
            }*/
        }
        
        for (int i = myTerrains.Count-1; i >= 0; i--) {
            if (myTerrains[i].needToBePurged) {
                myTerrains[i].terrain.GetComponent<PooledObject>().DestroyPooledObject();
                myTerrains.RemoveAt(i);
                if (!instant) {
                    yield return null;
                }
            }
        }
        reDrawing = false;

        while (currentTerrainGenCount > 0) {
            yield return null;
        }

        if (instant) {
            while (terrainStabilizedTimer > 0) {
                yield return new WaitForSeconds(0.2f);
                terrainGenerationProgress = Mathf.Lerp(terrainGenerationProgress, 1, 0.2f);
                if (terrainGenerationProgress >= 1) {
                    terrainGenerationProgress -= 0.1f;
                }
            }
        }
        
        terrainGenerationProgress = 1f;
        if (needReflectionProbe) {
            transform.parent.GetComponentInChildren<ReflectionProbe>().RenderProbe();
            needReflectionProbe = false;
        }
        //Debug.Break();
    }

    void ClearTerrains() {
        for (int i = myTerrains.Count-1; i >= 0; i--) {
            myTerrains[i].terrain.GetComponent<PooledObject>().DestroyPooledObject();
        }
        myTerrains.Clear();
    }


    Vector2Int GetSpiralNumber(int n) {
        // (di, dj) is a vector - direction in which we move right now
        int dx = 1;
        int dy = 0;
        // length of current segment
        int segment_length = 1;

        // current position (x, y) and how much of current segment we passed
        int x = 0;
        int y = 0;
        int segment_passed = 0;
        for (int k = 0; k < n; ++k) {
            // make a step, add 'direction' vector (dx, dy) to current position (x, y)
            x += dx;
            y += dy;
            ++segment_passed;

            if (segment_passed == segment_length) {
                // done with current segment
                segment_passed = 0;

                // 'rotate' directions
                int buffer = dx;
                dx = -dy;
                dy = buffer;

                // increase segment length if necessary
                if (dy == 0) {
                    ++segment_length;
                }
            }
        }

        return new Vector2Int(x, y);
    }

    public float terrainStabilizedTimer = -1f;
    void TerrainGenDone() {
        currentTerrainGenCount -= 1;
        /*if (myTerrains.Count >= 9) {
            StopCoroutine(nameof(StitchTerrains));
            StartCoroutine(StitchTerrains());
        }*/
        terrainStabilizedTimer = 1.5f;
    }

    /*[Button]
    void ReApplyAllDistanceMaps() {
        for (int i = 0; i < myTerrains.Count; i++) {
            myTerrains[i].needReDistance = true;
            GetComponent<TerrainGenerator>().RetryFinishing(myTerrains[i]);
        }
    }*/
    
    /*[Button]
    IEnumerator StitchTerrains() {
        while (GetComponent<TerrainGenerator>().isStitching) {
            yield return null;
        }
        Debug.Log("========== Try stitch terrains");
        GetComponent<TerrainGenerator>().SyncTerrainEdgesAndRecalculateHeightmaps(FullyComplete);
    }

    void FullyComplete() {
        var allDone = true;
        for (int i = 0; i < myTerrains.Count; i++) {
            if (myTerrains[i].needReDraw) {
                allDone = false;
            }
        }

        if (allDone) {
            _stopwatch.Stop();
            Debug.Log($"Total time: {_stopwatch.ElapsedMilliseconds}ms");
        }
    }*/

    public TerrainGenerator.TrainTerrain GetTerrainAtCoordinates(Vector2Int point) {
        for (int i = 0; i < myTerrains.Count; i++) {
            if (myTerrains[i].coordinates.x == point.x && myTerrains[i].coordinates.y == point.y) {
                return myTerrains[i];
            }
        }

        return null;
    }
    
    
    
    /*[Button]
    public void MakeTracksEditor() {
        for (int i = 0; i < myTracks.Count; i++) {
            if (myTracks[myTracks.Count - 1 - i] != null) {
                DestroyImmediate(myTracks[myTracks.Count - 1 - i]);
            }
        }
        myTracks.Clear();

        for (int i = 0; i < myPaths.Count; i++) {
            var trainPath = myPaths[i];
            var distance = 0f;
            while (distance < trainPath.length) {
                var newTrack = Instantiate(track,trackParent);
                newTrack.transform.position = PathGenerator.GetPointOnLine(trainPath, distance);
                newTrack.transform.rotation = PathGenerator.GetDirectionOnTheLine(trainPath, distance);
                myTracks.Add(newTrack);

                distance += trackDistance;
            }
        }
    }*/

    public ObjectPool trackPool;
    public ObjectPool bentTrackPool;
    private PathGenerator _pathGenerator => GetComponent<PathGenerator>();

    private bool makingTracks = false;
    public IEnumerator MakeTracksAroundCenter(bool instant = false) {
        makingTracks = true;
        var viewBound = new Bounds(drawCenter, Vector3.one * terrainViewRange*2);
        DebugDrawBounds(viewBound, Color.magenta, 3f);

        var n = 0;
        for (int i = 0; i < myPaths.Count; i++) {
            var trainPath = myPaths[i];
            //print(trainPath.bounds);
            if (trainPath.bounds.Intersects(viewBound)) {
                DebugDrawBounds(trainPath.bounds, Color.green, 3f);
                var index = 0;
                
                if (!myTracks.ContainsKey(trainPath.trackObjectsId)) {
                    myTracks[trainPath.trackObjectsId] = new List<GameObject>();
                    
                    var curTracks = myTracks[trainPath.trackObjectsId];

                    while (index +4 < trainPath.points.Length) {
                        var point = trainPath.points[index];
                        var direction = trainPath.points[index + 1] - trainPath.points[index];
                        var directionFurther = trainPath.points[index + 4] - trainPath.points[index];
                        var rotation = Quaternion.LookRotation(direction);

                        GameObject newTrack;
                        if (Vector3.Angle(direction, directionFurther) > 0) {
                            newTrack = bentTrackPool.Spawn(point, rotation);
                            //Debug.DrawLine(newTrack.transform.position, newTrack.transform.position+direction,Color.red, 1000f);
                            //Debug.DrawLine(newTrack.transform.position, newTrack.transform.position+directionFurther,Color.blue, 1000f);
                            //Debug.DrawLine(newTrack.transform.position, newTrack.transform.position+Vector3.Cross(direction, directionFurther).normalized,Color.green, 1000f);
                            if (Vector3.Cross(direction, directionFurther).y > 0) {
                                newTrack.transform.localScale = new Vector3(-1, 1, 1);
                            } else {
                                newTrack.transform.localScale = new Vector3(1, 1, 1);
                            }
                        } else {
                            newTrack = trackPool.Spawn(point, rotation);
                        }

                        curTracks.Add(newTrack);

                        index += 5;
                    
                        if (!instant && n % 20 == 0 && !TimeController.s.fastForwarding) {
                            yield return null;
                        }

                        n++;
                    }
                }
            } else {
                DebugDrawBounds(trainPath.bounds, Color.red, 3f);
                if (myTracks.ContainsKey(trainPath.trackObjectsId)) {
                    var listOfTracks = myTracks[trainPath.trackObjectsId];
                    for (int j = 0; j < listOfTracks.Count; j++) {
                        listOfTracks[j].GetComponent<PooledObject>().DestroyPooledObject();
                    }
                }
                myTracks.Remove(trainPath.trackObjectsId);
            }
        }

        makingTracks = false;
        yield return null;
    }
    
    void DebugDrawBounds(Bounds b,Color color, float delay=0)
    {
        return;
        // bottom
        var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
        var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
        var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
        var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

        Debug.DrawLine(p1, p2, color, delay);
        Debug.DrawLine(p2, p3, color, delay);
        Debug.DrawLine(p3, p4, color, delay);
        Debug.DrawLine(p4, p1, color, delay);

        // top
        var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
        var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
        var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
        var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

        Debug.DrawLine(p5, p6, color, delay);
        Debug.DrawLine(p6, p7, color, delay);
        Debug.DrawLine(p7, p8, color, delay);
        Debug.DrawLine(p8, p5, color, delay);

        // sides
        Debug.DrawLine(p1, p5, color, delay);
        Debug.DrawLine(p2, p6, color, delay);
        Debug.DrawLine(p3, p7, color, delay);
        Debug.DrawLine(p4, p8, color, delay);
    }


    public bool debugDrawAllGizmos = false;
    private void OnDrawGizmosSelected() {
        for (int i = 0; i < myPaths.Count; i++) {
            if (!debugDrawAllGizmos) {
                if (!myPaths[i].debugDrawGizmo) {
                    continue;
                }
            }

            var path = myPaths[i].points;
            Vector3 startPoint = transform.position;
            Vector3 direction = Vector3.forward;
            Gizmos.color = Color.white;
            for (int k = 0; k < path.Length-1; k++) {
                //if (PathGenerator.CircularDistanceToLine(startPoint, direction, path[k]) > GetComponent<PathGenerator>().pathWidth) {
                if (PathGenerator.CircularDistanceToLine(Vector3.left*100,  path[k]) > _pathGenerator.circularPathWidth) {
                    Gizmos.color = Color.red;
                } else {
                    Gizmos.color = Color.white;
                }
                
                /*if(myPaths[i].myType == PathGenerator.TrainPath.PathType.end)
                    Gizmos.color = Color.green;*/

                Gizmos.DrawLine(path[k]+Vector3.up, path[k+1]+Vector3.up);    
            }
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(myPaths[i].bounds.center, myPaths[i].bounds.size);

            /*Gizmos.color = Color.red;
            Gizmos.DrawSphere(path[myPaths[i].endPoint], 2);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(path[myPaths[i].rotateStopPoint], 2);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(path[myPaths[i].endRotateStartPoint], 2);*/
        }
        
        
        for (int i = 0; i < cityStampPaths.Count; i++) {
            var path = cityStampPaths[i].points;
            Gizmos.color = Color.cyan;
            for (int k = 0; k < path.Length-1; k++) {
                Gizmos.DrawLine(path[k]+Vector3.up, path[k+1]+Vector3.up);    
            }
        }
    }
    
    [Button]
    private void DebugDrawAllPaths() {
        for (int i = 0; i < myPaths.Count; i++) {
            var path = myPaths[i].points;
            for (int k = 0; k < path.Length-1; k++) {
                Debug.DrawLine(path[k]+Vector3.up, path[k+1]+Vector3.up, Color.white, 1000f);    
            }
        }
        
        
        for (int i = 0; i < cityStampPaths.Count; i++) {
            var path = cityStampPaths[i].points;
            for (int k = 0; k < path.Length-1; k++) {
                Debug.DrawLine(path[k]+Vector3.up, path[k+1]+Vector3.up, Color.cyan, 1000f);    
            }
        }
    }

    [Button]
    void _DebugDisableAllGizmoDraw() {
        for (int i = 0; i < myPaths.Count; i++) {
            myPaths[i].debugDrawGizmo = false;
        }
    }

    private Vector3 prevDrawCenter;
    private void Update() {
        if (terrainStabilizedTimer > 0) {
            terrainStabilizedTimer -= Time.deltaTime;

            if (terrainStabilizedTimer <= 0) {
                initialTerrainMade = true;
                //print("terrain stabilized");
                OnNewTerrainStabilized?.Invoke();
            }
        }
        
        if (!PlayStateMaster.s.isCombatStarted()) {
            return;
        }

        /*var point = GetPointOnActivePath(0);

        for (int i = 0; i < myPaths.Count; i++) {
            var points = myPaths[i].points;
            for (int j = 0; j < points.Length; j++) {
                points[j] -= point;
            }

            myPaths[i].bounds.center -= point;
        }
        
        for (int i = 0; i < cityStampPaths.Count; i++) {
            var points = cityStampPaths[i].points;
            for (int j = 0; j < points.Length; j++) {
                points[j] -= point;
            }

            cityStampPaths[i].bounds.center -= point;
        }
        

        /*for (int i = 0; i < myTerrains.Count; i++) {
            myTerrains[i].terrain.transform.position -= point;
            myTerrains[i].topLeftPos -= point;
            myTerrains[i].bounds.center -= point;
        }#1#
        transform.position -= point;

        for (int i = 0; i < myTerrains.Count; i++) {
            myTerrains[i].AddToGrassPositions(-point);
            myTerrains[i].topLeftPosition = myTerrains[i].terrain.GetPosition();
        }

        drawCenter -= point;
        center -= point;*/

        drawCenter = GetPointOnActivePath(0);
        //if (Mathf.Abs(drawCenter.x)  > terrainWidth/2f || Mathf.Abs(drawCenter.z)  > terrainWidth/2f) {
        if(Vector3.Distance(drawCenter, prevDrawCenter) > terrainWidth/2f) {
            prevDrawCenter = drawCenter;
            StartCoroutine(ReDrawTerrainAroundCenter());
        }
    }


    // 0 is train center
    public Vector3 GetPointOnActivePath(float currentDistanceOffset) {
        var currentDistance = GetCurrentDistance(currentDistanceOffset);
        var correctPath = GetCorrectPath(ref currentDistance);
        
        if(correctPath == null)
            return Vector3.forward*currentDistance;

        return PathGenerator.GetPointOnLine(correctPath, currentDistance);
    }
    
    public Quaternion GetRotationOnActivePath(float currentDistanceOffset) {
        var currentDistance = GetCurrentDistance(currentDistanceOffset);
        var correctPath = GetCorrectPath(ref currentDistance);
        
        if(correctPath == null)
            return Quaternion.identity;

        return PathGenerator.GetDirectionOnTheLine(correctPath, currentDistance);
    }
    
    public Vector3 GetDirectionVectorOnActivePath(float currentDistanceOffset) {
        var currentDistance = GetCurrentDistance(currentDistanceOffset);
        var correctPath = GetCorrectPath(ref currentDistance);
        
        if(correctPath == null)
            return Vector3.zero;

        return PathGenerator.GetDirectionVectorOnTheLine(correctPath, currentDistance);
    }

    float GetCurrentDistance(float currentDistanceOffset) {
        var currentDistance = SpeedController.s.currentDistance + currentDistanceOffset;
        currentDistance -= currentPathTreeOffset;
        return currentDistance;
    }
    PathGenerator.TrainPath GetCorrectPath(ref float currentDistance) {
        if (currentPathTree == null) {
            if (myPaths.Count > 0) {
                return myPaths[0];
            }
            return null;
        }

        var travelPathTree = currentPathTree;
        if (currentDistance > 0) {
            while (currentDistance > travelPathTree.myPath.length) {
                currentDistance -= travelPathTree.myPath.length;
                if (PathSelectorController.s.mainLever.topSelected) {
                    if (travelPathTree.leftPathTree != null) {
                        travelPathTree = travelPathTree.leftPathTree;
                    } else {
                        currentDistance += travelPathTree.myPath.length;
                        break;
                    }
                } else {
                    if (travelPathTree.rightPathTree != null) {
                        travelPathTree = travelPathTree.rightPathTree;
                    } else {
                        currentDistance += travelPathTree.myPath.length;
                        break;
                    }
                }
            }
        } else {
            if (travelPathTree.prevPathTree != null) {
                travelPathTree = travelPathTree.prevPathTree;
                while (-currentDistance > travelPathTree.myPath.length) {
                    currentDistance += travelPathTree.myPath.length;
                    if (travelPathTree.prevPathTree != null) {
                        travelPathTree = travelPathTree.prevPathTree;
                    } else {
                        currentDistance -= travelPathTree.myPath.length;
                        break;
                    }
                }
                currentDistance += travelPathTree.myPath.length;
            }
        }

        return travelPathTree.myPath;
    }


    [Button]
    void DebugApplySunAndSkyboxEditor(int biome) {
        currentBiomeIndex = biome;
        var currentBiome = biomes[currentBiomeIndex];
        ApplyBiome(currentBiome);
    }

    public int GetCurrentBiomeIndex() {
        return currentBiomeIndex;
    }
    void ApplyBiome(Biome currentBiome) {
        for (int i = 0; i < biomes.Length; i++) {
            biomes[i].sun.gameObject.SetActive(false);
        }
        
        currentBiome.skybox.SetActiveSkybox(currentBiome.sun, null);

        RenderSettings.ambientIntensity = currentBiome.skyboxLightIntensity;
        
    }
}
