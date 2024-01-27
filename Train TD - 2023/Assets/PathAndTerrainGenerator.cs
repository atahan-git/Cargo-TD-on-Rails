using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
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
    }

    public List<PathGenerator.TrainPath> myPaths = new List<PathGenerator.TrainPath>();
    public List<TerrainGenerator.TrainTerrain> myTerrains = new List<TerrainGenerator.TrainTerrain>();
    public Dictionary<Vector3, GameObject> myTracks = new Dictionary<Vector3,GameObject>();


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
        public PathTree prevPath;
        public PathGenerator.TrainPath myPath;
        public PathTree leftPath;
        public PathTree rightPath;

        public bool startPath = false;
        public bool endPath = false;
    }

    private void Start() {
        currentTerrainGenCount = 0;
        SetBiomes();
        MakeStarterAreaTerrain();
    }

    public void SetBiomes() {
        if (!DataSaver.s.GetCurrentSave().isInARun)
            biomeOverride = 0;

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
        var fakePath = GetComponent<PathGenerator>().MakeStationPath(Vector3.zero, Vector3.forward,Vector3.forward, SpeedController.s.missionDistance + 100);
        myPaths.Add(fakePath);
        terrainViewRange = 50;
        //StartCoroutine(ReDrawTerrainAroundCenter());
    }


    public float terrainGenerationProgress = 0;
    private bool needReflectionProbe = false;
    public void MakeStarterAreaTerrain() {
        terrainGenerationProgress = 0;
        needReflectionProbe = true;
        ClearTerrains();
        myPaths.Clear();
        terrainViewRange = 20;
        myPaths.Add(GetComponent<PathGenerator>().MakeStationPath(-Vector3.forward*PathGenerator.stationStraightDistance/2f,Vector3.forward, Vector3.forward, 0));
        StartCoroutine(ReDrawTerrainAroundCenter());
    }

    [Button]
    public void DebugMakePath() {
        myPaths.Clear();
        myPaths.Add(GetComponent<PathGenerator>().MakeStationPath(transform.position-Vector3.forward*PathGenerator.stationStraightDistance/2f,Vector3.forward, Vector3.forward, PathGenerator.stationStraightDistance));
        
        var startPoint = myPaths[0].points[^1];

        var segmentDistance = 200;
        var segmentDirection = Quaternion.Euler(0,Random.Range(-15, 15),0) * Vector3.forward;

        var path = GetComponent<PathGenerator>().MakeTrainPath(startPoint, Vector3.forward, segmentDirection, segmentDistance);
        myPaths.Add(path);
        
        var currentPathTree = new PathTree() {
            myPath = path
        };
        currentPathTreeOffset = PathGenerator.stationStraightDistance/2f;
        
        ForkPath(currentPathTree, 0,3);
    }


    [Button]
    public void DebugMakeCircleTerrain() {
        myPaths.Clear();
        var isGoodTrack = false;

        PathGenerator.TrainPath track = null;
        var n = 0;
        while (!isGoodTrack) {
            track = GetComponent<PathGenerator>().MakeCirclePath(Vector3.left * 100);
            isGoodTrack = Vector3.Distance(track.points[0], track.points[^1]) < 1;
            n++;

            if (n > 20) {
                Debug.LogError("Could not make a good track in under 20 tries");
                break;
            }
        }
        myPaths.Add(track);
    }

    [Button]
    public void DebugMakeTerrain() {
        var viewCount = Mathf.CeilToInt(20 * 2f / TerrainGenerator.terrainWidth);
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
                myPaths,
                TerrainGenDone
            );
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

        var startPoint = myPaths[0].points[^1];

        var segmentDistance = activeLevel.GetRandomSegmentLength() - (PathGenerator.stationStraightDistance/2f);
        var segmentDirection = Quaternion.Euler(0,Random.Range(-15, 15),0) * Vector3.forward;

        var path = GetComponent<PathGenerator>().MakeTrainPath(startPoint, Vector3.forward, segmentDirection, segmentDistance);
        myPaths.Add(path);

        currentPathTree = new PathTree() {
            myPath = path
        };
        currentPathTreeOffset = PathGenerator.stationStraightDistance/2f;
        
        ForkPath(currentPathTree, 1,3);
        ExtendAndPruneTerrain();
        StartCoroutine(ReDrawTerrainAroundCenter());
    }

    public void ExtendAndPruneTerrain() {
        
    }


    private float endStationChance = 0.25f;
    void ForkPath(PathTree path, int segmentIndex, int maxDepth) {
        var startPoint = path.myPath.points[^1];
        var startDirection = PathGenerator.GetDirectionVectorOnTheLine(path.myPath, path.myPath.length);

        var makeEndStation = Random.value < endStationChance;
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
        {
            var leftSegmentDirection = Quaternion.Euler(0,  30 + Random.Range(-15, 15), 0) * startDirection;
            path.leftPath = _MakeForkedPath(path, segmentIndex, maxDepth, leftStationMakeEnd, startPoint, startDirection, leftSegmentDirection, segmentDistance);
        }
        //right fork
        {
            var rightSegmentDirection = Quaternion.Euler(0, - 30 + Random.Range(-15, 15), 0) * startDirection;
            path.rightPath = _MakeForkedPath(path, segmentIndex, maxDepth, rightStationMakeEnd, startPoint, startDirection, rightSegmentDirection, segmentDistance);
        }
    }

    private PathTree _MakeForkedPath(PathTree path, int segmentIndex, int maxDepth, bool leftStationMakeEnd, Vector3 startPoint, Vector3 startDirection, Vector3 segmentDirection, float segmentDistance) {
        if (leftStationMakeEnd) {
            var newStationEndPath = GetComponent<PathGenerator>().MakeStationPath(startPoint, startDirection, segmentDirection, segmentDistance, true);
            myPaths.Add(newStationEndPath);
            var newStationEndPathTree = new PathTree() {
                prevPath = path,
                myPath = newStationEndPath,
                endPath = true
            };
            return newStationEndPathTree;
        } else {
            var newPath = GetComponent<PathGenerator>().MakeTrainPath(startPoint, startDirection, segmentDirection, segmentDistance, true);
            myPaths.Add(newPath);
            var newPathTree = new PathTree() {
                prevPath = path,
                myPath = newPath
            };

            if (segmentIndex + 1 < maxDepth) {
                ForkPath(newPathTree, segmentIndex + 1, maxDepth);
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

        StartCoroutine(MakeTracksAroundCenter());

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
                    myPaths,
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

    void TerrainGenDone() {
        currentTerrainGenCount -= 1;
        /*if (myTerrains.Count >= 9) {
            StopCoroutine(nameof(StitchTerrains));
            StartCoroutine(StitchTerrains());
        }*/
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
    public IEnumerator MakeTracksAroundCenter() {
        var viewBound = new Bounds(Vector3.zero, Vector3.one * terrainViewRange*2);
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
                if (PathGenerator.CircularDistanceToLine(Vector3.left*100,  path[k]) > GetComponent<PathGenerator>().circularPathWidth) {
                    Gizmos.color = Color.red;
                } else {
                    Gizmos.color = Color.white;
                }
            
                Gizmos.DrawLine(path[k]+Vector3.up, path[k+1]+Vector3.up);    
            }
            
            /*Gizmos.color = Color.red;
            Gizmos.DrawSphere(path[myPaths[i].endPoint], 2);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(path[myPaths[i].rotateStopPoint], 2);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(path[myPaths[i].endRotateStartPoint], 2);*/
        }
    }

    private void Update() {
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
        var currentDistance = SpeedController.s.currentDistance + currentDistanceOffset;
        currentDistance -= currentPathTreeOffset;
        if (currentPathTree == null) {
            if (myPaths.Count > 0) {
                return PathGenerator.GetPointOnLine(myPaths[0], currentDistance);
            }
            return Vector3.forward * currentDistance;
        }
        
        return PathGenerator.GetPointOnLine(currentPathTree.myPath, currentDistance);
    }
    
    public Quaternion GetRotationOnActivePath(float currentDistanceOffset) {
        var currentDistance = SpeedController.s.currentDistance + currentDistanceOffset;
        currentDistance -= currentPathTreeOffset;
        if (currentPathTree == null) {
            if (myPaths.Count > 0) {
                return PathGenerator.GetDirectionOnTheLine(myPaths[0], currentDistance);
            }
            return Quaternion.identity;
        }
        
        return PathGenerator.GetDirectionOnTheLine(currentPathTree.myPath, currentDistance);
    }
    
    public Vector3 GetDirectionVectorOnActivePath(float currentDistanceOffset) {
        var currentDistance = SpeedController.s.currentDistance + currentDistanceOffset;
        currentDistance -= currentPathTreeOffset;
        if (currentPathTree == null) {
            if (myPaths.Count > 0) {
                return PathGenerator.GetDirectionVectorOnTheLine(myPaths[0], currentDistance);
            }
            return Vector3.zero;
        }
        
        return PathGenerator.GetDirectionVectorOnTheLine(currentPathTree.myPath, currentDistance).normalized;
    }
}
