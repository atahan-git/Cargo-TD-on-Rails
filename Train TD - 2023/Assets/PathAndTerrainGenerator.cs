using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
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

        myTracks.Clear();
        myTerrains.Clear();
        currentTerrainGenCount = 0;
        SetBiomes();
    }

    public List<PathGenerator.TrainPath> myPaths = new List<PathGenerator.TrainPath>();
    public List<TerrainGenerator.TrainTerrain> myTerrains = new List<TerrainGenerator.TrainTerrain>();
    public Dictionary<Vector3, GameObject> myTracks = new Dictionary<Vector3,GameObject>();
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

    public Biome[] biomes;
	
    [System.Serializable]
    public class Biome {
        public GameObject terrainPrefab;
        public Light sun;
        public SkyboxParametersScriptable skybox;
    }

    public int biomeOverride = -1;

    public ObjectPool terrainPool;

    public float trackDistance;

    public PathTree currentPathTree;
    public float currentPathTreeOffset = 0;
    
    [Serializable]
    public class PathTree {
        public PathTree prevPathTree;
        public PathGenerator.TrainPath myPath;
        public PathTree leftPathTree;
        public PathTree rightPathTree;

        public bool startPath = false;
        public bool endPath = false;

        public PathGenerator.TrainPath cityStampPath;
    }


    public UnityEvent OnNewTerrainStabilized = new UnityEvent();
    public bool initialTerrainMade = false;


    public void SetBiomes() {
        Biome currentBiome;
        if (biomeOverride < 0) {
            var targetBiome = 0;
            if (targetBiome < 0 || targetBiome > biomes.Length) {
                Debug.LogError($"Illegal biome {targetBiome}");
                targetBiome = 0;
            }

            currentBiome = biomes[targetBiome];
        } else {
            currentBiome = biomes[biomeOverride];
        }

        for (int i = 0; i < biomes.Length; i++) {
            biomes[i].sun.gameObject.SetActive(false);
        }
        
        currentBiome.skybox.SetActiveSkybox(currentBiome.sun, null);

        terrainPool.RePopulateWithNewObject(currentBiome.terrainPrefab);
    }


    public void MakeFakePathForMissionRewards() {
        terrainGenerationProgress = 0;
        needReflectionProbe = true;
        ClearTerrains();
        myPaths.Clear();
        cityStampPaths.Clear();
        terrainViewRange = 20;

        var stopDistance = 7.5f;

        var segmentDistance = 5f;
        var endStationPath = _pathGenerator.MakeStationAtBeginningPath(-Vector3.forward * PathGenerator.stationStraightDistance / 2f, Vector3.forward,Vector3.forward, segmentDistance);
        myPaths.Add(endStationPath);
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

        StartCoroutine(ReDrawTerrainAroundCenter());
    }


    [Button]
    public void DebugMakeCityStamp(Vector3 offset, Quaternion direction) {
        cityStampPaths.Clear();
        var cityPath = _pathGenerator.MakeCityStampPath(offset, direction);
        cityStampPaths.Add(cityPath);
        isComboListDirty = true;
    }

    [Button]
    public void DebugMakeCityPath() {
        myPaths.Clear();
        var starterStationPath = _pathGenerator.MakeStationAtBeginningPath(-Vector3.forward * PathGenerator.stationStraightDistance / 2f, Vector3.forward, Vector3.forward, Random.Range(30,50));
        myPaths.Add(starterStationPath);
        isComboListDirty = true;
    }


    public float terrainGenerationProgress = 0;
    private bool needReflectionProbe = false;
    public void MakeStarterAreaTerrain() {
        if (!PlayStateMaster.s.isMainMenu() && DataSaver.s.GetCurrentSave().isInEndRunArea) {
            MakeFakePathForMissionRewards();
            return;
        }
        
        terrainGenerationProgress = 0;
        needReflectionProbe = true;
        ClearTerrains();
        myPaths.Clear();
        cityStampPaths.Clear();
        terrainViewRange = 20;
        
        var starterStationPath = _pathGenerator.MakeStationAtBeginningPath(-Vector3.forward * PathGenerator.stationStraightDistance / 2f, Vector3.forward, Vector3.forward, Random.Range(30,50));
        myPaths.Add(starterStationPath);
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
        generatedPathDepth = 0;
        
        StartCoroutine(ReDrawTerrainAroundCenter());
    }


    [Button]
    public void DebugMakeCirclePath() {
        myPaths.Clear();
        var isGoodTrack = false;

        PathGenerator.TrainPath track = null;
        var n = 0;
        while (!isGoodTrack) {
            track = _pathGenerator.MakeCirclePath(Vector3.left * 100);
            isGoodTrack = Vector3.Distance(track.points[0], track.points[^1]) < 1;
            n++;

            if (n > 20) {
                Debug.LogError("Could not make a good track in under 20 tries");
                break;
            }
        }
        Debug.Log($"made good path in {n} tries");
        myPaths.Add(track);
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
                center,
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
        var viewBound = new Bounds(Vector3.zero, Vector3.one * terrainViewRange);

        /*for (int i =  myTracks.Count-1; i >=0; i--) {
            myTracks[i].GetComponent<PooledObject>().DestroyPooledObject();
        }
        myTracks.Clear();*/

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
                        var newTrack = TrackPool.Spawn(point, PathGenerator.GetDirectionOnTheLine(trainPath, distance));
                        myTracks[point-center] = newTrack;
                    }
                    distance += trackDistance;
                }
            }
        }
    }

    
    private ConstructedLevel activeLevel => PlayStateMaster.s.currentLevel;
    public void MakeLevelTerrain() {
        if (activeLevel == null) {
            return;
        }
        terrainViewRange = 50;
        
        PruneAndExtendPaths();
        StartCoroutine(ReDrawTerrainAroundCenter());
    }

    public const float pathGenerateDistance = 600;
    public void PruneAndExtendPaths() {
        
        // extend
        ForkPath(currentPathTree, 0, pathGenerateDistance);

        // prune
        PrunePath(currentPathTree, 0, pathGenerateDistance,new List<PathTree>());
    }


    void PrunePath(PathTree pathTree, float curDistance, float maxDistance, List<PathTree> processedList) {
        if(pathTree == null)
            return;
        if(processedList.Contains(pathTree))
        {
            return;
        }

        if (curDistance > maxDistance) {
            myPaths.Remove(pathTree.myPath);
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

        PrunePath(pathTree.prevPathTree, curDistance, maxDistance, processedList);
        PrunePath(pathTree.leftPathTree, curDistance, maxDistance, processedList);
        PrunePath(pathTree.rightPathTree, curDistance, maxDistance, processedList);
    }


    public int generatedPathDepth = 0; // min path before we generate an end station
    public int minCastleDepth = 2;
    private float endStationChance = 0.25f;
    private bool forceEndStation = true;
    void ForkPath(PathTree pathTree, float curDistance, float maxDistance) {
        if(pathTree.myPath.endPath)
            return;
        
        var startPoint = pathTree.myPath.points[^1];
        var startDirection = PathGenerator.GetDirectionVectorOnTheLine(pathTree.myPath, pathTree.myPath.length);
        
        var makeEndStation = generatedPathDepth > (Mathf.Pow(2,minCastleDepth)) && Random.value < endStationChance;
        if (forceEndStation) {
            makeEndStation = true;
        }
        var leftStationMakeEnd = false;
        var rightStationMakeEnd = false;
        if (makeEndStation) {
            if (Random.value < 0.5f) {
                leftStationMakeEnd = true;
            } else {
                rightStationMakeEnd = true;
            }
        }
        
        var segmentDistance = activeLevel.GetRandomSegmentLength();
        // left fork
        if(pathTree.leftPathTree == null) {
            generatedPathDepth += 1;
            var leftSegmentDirection = Quaternion.Euler(0, - 30 + Random.Range(-15, 15), 0) * startDirection;
            pathTree.leftPathTree = _MakeForkedPath(pathTree, curDistance, maxDistance, leftStationMakeEnd, startPoint, startDirection, leftSegmentDirection, segmentDistance);
        }
        //right fork
        if(pathTree.rightPathTree == null) {
            var rightSegmentDirection = Quaternion.Euler(0,  30 + Random.Range(-15, 15), 0) * startDirection;
            pathTree.rightPathTree = _MakeForkedPath(pathTree, curDistance, maxDistance, rightStationMakeEnd, startPoint, startDirection, rightSegmentDirection, segmentDistance);
        }
    }

    private float endStationCenterOffset = 14;
    private PathTree _MakeForkedPath(PathTree pathTree, float curDistance, float maxDistance, bool leftStationMakeEnd, Vector3 startPoint, Vector3 startDirection, Vector3 segmentDirection, float segmentDistance) {
        if (leftStationMakeEnd) {
            var newStationEndPath = _pathGenerator.MakeStationAtEndPath(startPoint, startDirection, segmentDirection, segmentDistance, true);
            var cityDistance = segmentDistance + PathGenerator.stationStraightDistance/2f + endStationCenterOffset;
            var cityStampPath = _pathGenerator.MakeCityStampPath(PathGenerator.GetPointOnLine(newStationEndPath, cityDistance), PathGenerator.GetDirectionOnTheLine(newStationEndPath, cityDistance));
            myPaths.Add(newStationEndPath);
            isComboListDirty = true;
            var newStationEndPathTree = new PathTree() {
                prevPathTree = pathTree,
                myPath = newStationEndPath,
                endPath = true,
                cityStampPath = cityStampPath
            };
            cityStampPaths.Add(cityStampPath);
            isComboListDirty = true;
            return newStationEndPathTree;
        } else {
            var newPath = _pathGenerator.MakeTrainPath(startPoint, startDirection, segmentDirection, segmentDistance, true);
            myPaths.Add(newPath);
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
        }
    }
    
    


    /*[Button]
    public void MakePath() {
        myPaths.Add(GetComponent<PathGenerator>().MakeTrainPath(transform.position-Vector3.forward*100,Vector3.forward, 200));
    }*/

    public Vector3 center = Vector3.zero;
    public Vector3 drawCenter = Vector3.zero;
    private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();

    private int terrainWidth => TerrainGenerator.terrainWidth;
    private int currentTerrainGenCount = 0;
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
    IEnumerator ReDrawTerrainAroundCenter() {
        while (reDrawing) {
            yield return null;
        }

        //Debug.Break();
        reDrawing = true;
        
        while (drawCenter.x > terrainWidth/2f) {
            drawCenter.x -= terrainWidth;
        }

        while (drawCenter.x < -terrainWidth/2f) {
            drawCenter.x += terrainWidth;
        }
        
        while (drawCenter.z > terrainWidth/2f) {
            drawCenter.z -= terrainWidth;
        }

        while (drawCenter.z < -terrainWidth/2f) {
            drawCenter.z += terrainWidth;
        }

        for (int i = 0; i < myTerrains.Count; i++) {
            myTerrains[i].needToBePurged = true;
        }

        if (!makingTracks) {
            StartCoroutine(MakeTracksAroundCenter());
        }

        var diff =  drawCenter-center;
        var viewCount = Mathf.CeilToInt(terrainViewRange * 2 / TerrainGenerator.terrainWidth);
        if (viewCount % 2 == 0) {
            viewCount += 1;
        }
        var terrainCount = (int)Mathf.Pow( viewCount,2);
        var addition = 1f / terrainCount;
        terrainGenerationProgress -= 0.1f;
        
        for (int i = 0; i < terrainCount; i++) {
            var offset = GetSpiralNumber(i);
            offset.x += Mathf.RoundToInt(diff.x / terrainWidth);
            offset.y += Mathf.RoundToInt(diff.z / terrainWidth);
            var coordinates = new Vector2Int(offset.x, offset.y);
            var existingTerrain = GetTerrainAtCoordinates(coordinates);
            //Debug.Log($"Trying to draw terrain at coords {coordinates.x}, {coordinates.y} with diff {diff/terrainWidth}");
            if (existingTerrain == null) {
                
                GetComponent<TerrainGenerator>().MakeTerrainDistanceMaps(
                    coordinates,
                    center,
                    comboList,
                    TerrainGenDone
                );
                currentTerrainGenCount += 1;
                yield return null;
                yield return null;
                yield return null;
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
                yield return null;
            }
        }
        reDrawing = false;

        while (currentTerrainGenCount > 0) {
            yield return null;
        }
        terrainGenerationProgress = 1f;
        if (needReflectionProbe) {
            transform.parent.GetComponentInChildren<ReflectionProbe>().RenderProbe();
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

    public ObjectPool TrackPool;
    private PathGenerator _pathGenerator => GetComponent<PathGenerator>();

    private bool makingTracks = false;
    public IEnumerator MakeTracksAroundCenter() {
        makingTracks = true;
        var viewBound = new Bounds(Vector3.zero, Vector3.one * terrainViewRange*2);
        var deleteList = new List<Vector3>();
        var n = 0;
        foreach (var keyValuePair in myTracks) {
            if (!viewBound.Contains(keyValuePair.Key+center)) {
                keyValuePair.Value.GetComponent<PooledObject>().DestroyPooledObject();
                deleteList.Add(keyValuePair.Key);
            }

            if (n % 300 == 0 && !TimeController.s.fastForwarding) {
                yield return null;
            }

            n++;
        }
        //print("key pruning done");

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
                        var newTrack = TrackPool.Spawn(point, PathGenerator.GetDirectionOnTheLine(trainPath, distance));
                        myTracks[point-center] = newTrack;
                    }

                    distance += trackDistance;
                    
                    if (n % 20 == 0 && !TimeController.s.fastForwarding) {
                        yield return null;
                    }

                    n++;
                }
            }
        }
        
        
        //print("track adding done");

        makingTracks = false;
        yield return null;
    }
    
    
    private void OnDrawGizmosSelected() {
        for (int i = 0; i < myPaths.Count; i++) {
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
                
                if(myPaths[i].endPath)
                    Gizmos.color = Color.green;

                Gizmos.DrawLine(path[k]+Vector3.up, path[k+1]+Vector3.up);    
            }
            
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

        var point = GetPointOnActivePath(0);

        for (int i = 0; i < myPaths.Count; i++) {
            var points = myPaths[i].points;
            for (int j = 0; j < points.Length; j++) {
                points[j] -= point;
            }
        }
        
        for (int i = 0; i < cityStampPaths.Count; i++) {
            var points = cityStampPaths[i].points;
            for (int j = 0; j < points.Length; j++) {
                points[j] -= point;
            }
        }
        

        /*for (int i = 0; i < myTerrains.Count; i++) {
            myTerrains[i].terrain.transform.position -= point;
            myTerrains[i].topLeftPos -= point;
            myTerrains[i].bounds.center -= point;
        }*/
        transform.position -= point;

        drawCenter -= point;
        center -= point;

        if (Mathf.Abs(drawCenter.x)  > terrainWidth/2f || Mathf.Abs(drawCenter.z)  > terrainWidth/2f) {
            StartCoroutine(ReDrawTerrainAroundCenter());
        }
    }


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
            if (currentDistance > travelPathTree.myPath.length) {
                currentDistance -= travelPathTree.myPath.length;
                if (PathSelectorController.s.mainLever.topSelected) {
                    if(travelPathTree.leftPathTree != null)
                        travelPathTree = travelPathTree.leftPathTree;
                } else {
                    if(travelPathTree.rightPathTree != null)
                        travelPathTree = travelPathTree.rightPathTree;
                }
            }
        } else {
            if (travelPathTree.prevPathTree != null) {
                travelPathTree = travelPathTree.prevPathTree;
                currentDistance += travelPathTree.myPath.length;
            }
        }

        return travelPathTree.myPath;
    }
}
