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
    public List<GameObject> myTracks = new List<GameObject>();

    public float trackDistance;

    public PathTree currentPathTree;
    
    [Serializable]
    public class PathTree {
        public PathGenerator.TrainPath myPath;
        public PathTree leftPath;
        public PathTree rightPath;
    }

    private void Start() {
        MakeStarterAreaTerrain();
    }


    public void MakeFakePathForMissionRewards() {
        var fakePath = GetComponent<PathGenerator>().MakeStationPath(Vector3.zero, Vector3.forward, SpeedController.s.missionDistance + 100);
        myPaths.Add(fakePath);
        activePath.Add(fakePath);
        terrainViewRange = 50;
        //StartCoroutine(ReDrawTerrainAroundCenter());
    }


    private const float stationStraightDistance = 100;
    public float terrainGenerationProgress = 0;
    private bool needReflectionProbe = false;
    public void MakeStarterAreaTerrain() {
        terrainGenerationProgress = 0;
        needReflectionProbe = true;
        ClearTerrains();
        myPaths.Clear();
        terrainViewRange = 20;
        myPaths.Add(GetComponent<PathGenerator>().MakeStationPath(-Vector3.forward*stationStraightDistance/2f,Vector3.forward, stationStraightDistance));
        StartCoroutine(ReDrawTerrainAroundCenter());
    }

    [Button]
    public void DebugMakePath() {
        myPaths.Clear();
        myPaths.Add(GetComponent<PathGenerator>().MakeStationPath(transform.position-Vector3.forward*stationStraightDistance/2f,Vector3.forward, stationStraightDistance));
        
        var startPoint = myPaths[0].points[^1];

        var segmentDistance = 200;
        var segmentDirection = Quaternion.Euler(0,Random.Range(-15, 15),0) * Vector3.forward;

        var path = GetComponent<PathGenerator>().MakeTrainPath(startPoint, Vector3.forward, segmentDirection, segmentDistance);
        myPaths.Add(path);
        
        var currentPathTree = new PathTree() {
            myPath = path
        };
        
        DebugForkPath(currentPathTree, 0, 0);
    }

    void DebugForkPath(PathTree path, int depth,float degreeOffset) {
        var startPoint = path.myPath.points[^1];
        var startDirection = PathGenerator.GetDirectionVectorOnTheLine(path.myPath, path.myPath.length);

        
        // left fork
        {
            var segmentDistance = Random.Range(250,350);
            var segmentDirection = Quaternion.Euler(0, degreeOffset + 30 + Random.Range(-15, 15), 0) * Vector3.forward;

            var leftPath = GetComponent<PathGenerator>().MakeTrainPath(startPoint, startDirection,segmentDirection, segmentDistance, true);
            myPaths.Add(leftPath);
            var leftTree = new PathTree() {
                myPath = leftPath
            };
            if (depth < 3) {
                DebugForkPath(leftTree, depth + 1, degreeOffset + 60);
            } else {
                var leftStationEnd = GetComponent<PathGenerator>().MakeStationPath(leftPath.points[^1], PathGenerator.GetDirectionVectorOnTheLine(leftPath, leftPath.length), stationStraightDistance);
                myPaths.Add(leftStationEnd);
                var leftStation = new PathTree() {
                    myPath = leftStationEnd
                };
                leftTree.rightPath = leftStation;
            }
            
            path.leftPath = leftTree;
        }
        //right fork
        {
            var segmentDistance = Random.Range(250,350);;
            var segmentDirection = Quaternion.Euler(0, degreeOffset - 30 + Random.Range(-15, 15), 0) * Vector3.forward;

            var rightPath = GetComponent<PathGenerator>().MakeTrainPath(startPoint, startDirection,segmentDirection, segmentDistance, true);
            myPaths.Add(rightPath);
            var rightTree = new PathTree() {
                myPath = rightPath
            };
            if (depth < 3) {
                DebugForkPath(rightTree, depth + 1, degreeOffset - 60);
            }else {
                var rightStationEnd = GetComponent<PathGenerator>().MakeStationPath(rightPath.points[^1], PathGenerator.GetDirectionVectorOnTheLine(rightPath, rightPath.length), stationStraightDistance);
                myPaths.Add(rightStationEnd);
                var rightStation = new PathTree() {
                    myPath = rightStationEnd
                };
                rightTree.rightPath = rightStation;
            }

            path.rightPath = rightTree;
        }
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

        for (int i =  myTracks.Count-1; i >=0; i--) {
            DestroyImmediate(myTracks[i].gameObject);
        }
        myTracks.Clear();
        
        for (int i = 0; i < myPaths.Count; i++) {
            if (myPaths[i].bounds.Intersects(viewBound)) {
                
                var trainPath = myPaths[i];
                var distance = 0f;
                while (distance < trainPath.length) {
                    var point = PathGenerator.GetPointOnLine(trainPath, distance);
                    if (viewBound.Contains(point)) {
                        var newTrack = TrackPool.Spawn(point, PathGenerator.GetDirectionOnTheLine(trainPath, distance));
                        myTracks.Add(newTrack);
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

        var segmentDistance = activeLevel.mySegmentsA[0].segmentLength - (stationStraightDistance/2f);
        var segmentDirection = Quaternion.Euler(0,Random.Range(-15, 15),0) * Vector3.forward;

        var path = GetComponent<PathGenerator>().MakeTrainPath(startPoint, Vector3.forward, segmentDirection, segmentDistance);
        myPaths.Add(path);

        currentPathTree = new PathTree() {
            myPath = path
        };
        
        ForkPath(currentPathTree, 1, 0);
        StartCoroutine(ReDrawTerrainAroundCenter());
    }

    void ForkPath(PathTree path, int segmentIndex, float degreeOffset) {
        var startPoint = path.myPath.points[^1];
        var startDirection = PathGenerator.GetDirectionVectorOnTheLine(path.myPath, path.myPath.length);

        
        // left fork
        {
            var segmentDistance = activeLevel.mySegmentsA[segmentIndex].segmentLength;
            var segmentDirection = Quaternion.Euler(0, degreeOffset + 30 + Random.Range(-15, 15), 0) * Vector3.forward;

            var leftPath = GetComponent<PathGenerator>().MakeTrainPath(startPoint, startDirection,segmentDirection, segmentDistance, true);
            myPaths.Add(leftPath);
            var leftTree = new PathTree() {
                myPath = leftPath
            };
            if (segmentIndex + 1 < activeLevel.mySegmentsA.Length) {
                ForkPath(leftTree, segmentIndex + 1, degreeOffset + 60);
            } else {
                var leftStationEnd = GetComponent<PathGenerator>().MakeStationPath(leftPath.points[^1], PathGenerator.GetDirectionVectorOnTheLine(leftPath, leftPath.length), stationStraightDistance);
                myPaths.Add(leftStationEnd);
                var leftStation = new PathTree() {
                    myPath = leftStationEnd
                };
                leftTree.rightPath = leftStation;
            }
            
            path.leftPath = leftTree;
        }
        //right fork
        {
            var segmentDistance = activeLevel.mySegmentsB[segmentIndex].segmentLength;
            var segmentDirection = Quaternion.Euler(0, degreeOffset - 30 + Random.Range(-15, 15), 0) * Vector3.forward;

            var rightPath = GetComponent<PathGenerator>().MakeTrainPath(startPoint, startDirection,segmentDirection, segmentDistance, true);
            myPaths.Add(rightPath);
            var rightTree = new PathTree() {
                myPath = rightPath
            };
            if (segmentIndex + 1 < activeLevel.mySegmentsA.Length) {
                ForkPath(rightTree, segmentIndex + 1, degreeOffset - 60);
            }else {
                var rightStationEnd = GetComponent<PathGenerator>().MakeStationPath(rightPath.points[^1], PathGenerator.GetDirectionVectorOnTheLine(rightPath, rightPath.length), stationStraightDistance);
                myPaths.Add(rightStationEnd);
                var rightStation = new PathTree() {
                    myPath = rightStationEnd
                };
                rightTree.rightPath = rightStation;
            }

            path.rightPath = rightTree;
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

        for (int i = 0; i < myTracks.Count; i++) {
            //if (!viewBound.Contains(myTracks[i].transform.position)) {
                myTracks[i].GetComponent<PooledObject>().DestroyPooledObject();
            //}
        }
        myTracks.Clear();
        
        for (int i = 0; i < myPaths.Count; i++) {
            if (myPaths[i].bounds.Intersects(viewBound)) {
                
                var trainPath = myPaths[i];
                var distance = 0f;
                while (distance < trainPath.length) {
                    var point = PathGenerator.GetPointOnLine(trainPath, distance);
                    if (viewBound.Contains(point)) {
                        var newTrack = TrackPool.Spawn(point, PathGenerator.GetDirectionOnTheLine(trainPath, distance));
                        myTracks.Add(newTrack);
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

    public List<PathGenerator.TrainPath> activePath = new List<PathGenerator.TrainPath>();
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
        var currentDistance = SpeedController.s.currentDistance + stationStraightDistance/2f + currentDistanceOffset;
        if (activePath.Count <= 0) {
            if (myPaths.Count > 0) {
                return PathGenerator.GetPointOnLine(myPaths[0], currentDistance);
            }
            return Vector3.zero;
        }
        var pathIndex = 0;
        while (currentDistance > activePath[pathIndex].length) {
            currentDistance -= activePath[pathIndex].length;
            pathIndex += 1;

            if (pathIndex >= activePath.Count) {
                pathIndex -= 1;
                currentDistance = activePath[pathIndex].length;
                break;
            }
        }
        return PathGenerator.GetPointOnLine(activePath[pathIndex], currentDistance);
    }
    
    public Quaternion GetRotationOnActivePath(float currentDistanceOffset) {
        var currentDistance = SpeedController.s.currentDistance + stationStraightDistance/2f + currentDistanceOffset;
        if (activePath.Count <= 0) {
            if (myPaths.Count > 0) {
                return PathGenerator.GetDirectionOnTheLine(myPaths[0], currentDistance);
            }
            return Quaternion.identity;
        }
        var pathIndex = 0;
        while (currentDistance > activePath[pathIndex].length) {
            currentDistance -= activePath[pathIndex].length;
            pathIndex += 1;

            if (pathIndex >= activePath.Count) {
                pathIndex -= 1;
                currentDistance = activePath[pathIndex].length;
                print("We went too far!");
                break;
            }
        }
        return PathGenerator.GetDirectionOnTheLine(activePath[pathIndex], currentDistance);
    }
    
    public Vector3 GetDirectionVectorOnActivePath(float currentDistanceOffset) {
        var currentDistance = SpeedController.s.currentDistance + stationStraightDistance/2f + currentDistanceOffset;
        if (activePath.Count <= 0) {
            if (myPaths.Count > 0) {
                return PathGenerator.GetDirectionVectorOnTheLine(myPaths[0], currentDistance);
            }
            return Vector3.zero;
        }
        var pathIndex = 0;
        while (currentDistance > activePath[pathIndex].length) {
            currentDistance -= activePath[pathIndex].length;
            pathIndex += 1;

            if (pathIndex >= activePath.Count) {
                pathIndex -= 1;
                currentDistance = activePath[pathIndex].length;
                break;
            }
        }
        return PathGenerator.GetDirectionVectorOnTheLine(activePath[pathIndex], currentDistance).normalized;
    }
}
