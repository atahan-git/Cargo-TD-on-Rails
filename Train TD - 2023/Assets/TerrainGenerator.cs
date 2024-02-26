using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;


public class TerrainGenerator : MonoBehaviour
{
    
    public AnimationCurve minecraftContinentalnessMap = AnimationCurve.Linear(0,0,1,1);
    public AnimationCurve continentalnessMap = AnimationCurve.Linear(0,0,1,1);
    public AnimationCurve transitionMap = AnimationCurve.Linear(0,0,1,1);

    public const int gridSize = 257;
    public const int terrainWidth = 25;
    public const int terrainHeight = 50;
    public const int maxDistance = gridSize*gridSize+1;
    public const float gridStep = ((float)terrainWidth)/(gridSize-1);
    public int detailGridSize;


    public float playZoneHeight = 0.3f;
    public float playZoneHeightVariance = 0.07f;
    public float maxMountainHeight = 10f;
    public float baseSideHeightIncrease = 1f;

    public float playZoneWidth = 8f;
    public float transitionZoneWidth = 1f;
    
    public Vector2 seed;

    [Serializable]
    public class TrainTerrain {
        public Vector2Int coordinates;
        public Vector3 topLeftPos;
        public Bounds bounds;
        //public Vector3[,] positionMap;
        public float[,] distanceMap;
        public float[,] heightmap;
        public int[,] detailmap0;
        public int[,] detailmap1;
        
        public Terrain terrain;
        public bool needReDraw = true;
        public bool needReDistance = false;
        public bool leftDirty = false;
        public bool rightDirty = false;
        public bool upDirty = false;
        public bool downDirty = false;
        public bool distanceBeingCalculated = false;
        public bool needReImprint = false;
        public bool beingFinished = false;
        public PathAndTerrainGenerator pathAndTerrainGenerator;
        public bool needToBePurged = false;
        public bool beingProcessed = false;

        public TreeInstance[] treeInstances;

        public float GetPos_X(int x, int y) {
            return x*gridStep + coordinates.x*terrainWidth;
        }
        public float GetPos_Y(int x, int y) {
            return y*gridStep + coordinates.y*terrainWidth;
        }

        public Vector3 GetRealPos(int x, int y) {
            return new Vector3(x * gridStep, 0, y * gridStep) + topLeftPos;
        }
    }
   

    public ObjectPool terrainPool;

    [Button]
    public void ChangeSeed() {
        seed = new Vector2(Random.Range(1000, 10000f), Random.Range(1000, 10000f));
    }

    private void Start() {
        var viewCount = Mathf.CeilToInt(50f * 2 / TerrainGenerator.terrainWidth);
        if (viewCount % 2 == 0) {
            viewCount += 1;
        }
        var terrainCount = (int)Mathf.Pow( viewCount,2);
        
        terrainPool.ExpandPoolToSize(Mathf.CeilToInt(terrainCount * 1.35f));

        var allTerrains = terrainPool.GetAllObjs();
        
        for (int i = 0; i < allTerrains.Length; i++) {
            var trainTerrainData = allTerrains[i].GetComponent<TrainTerrainData>();
            if (!trainTerrainData.isInitialized) {
                trainTerrainData.InitializeData();
            }
        }
    }

    public void MakeTerrainDistanceMaps(Vector2Int coordinates, Vector3 currentOffset, List<PathGenerator.TrainPath> paths, Action completeCallback) {
        //ChangeSeed();
        //var stopwatch = new System.Diagnostics.Stopwatch();
        //stopwatch.Start();

        var terrain = terrainPool.Spawn(Vector3.one*1000).GetComponent<Terrain>();
        var trainTerrainData = terrain.GetComponent<TrainTerrainData>();
        var terrainInformation = trainTerrainData.data;
        terrain.gameObject.name = $"Terrain {coordinates.x}, {coordinates.y}";
        
        detailGridSize = terrain.terrainData.detailWidth;
        
        if (!trainTerrainData.isInitialized) {
            trainTerrainData.InitializeData();
        }

        terrainInformation.coordinates = coordinates;
        terrainInformation.needToBePurged = false;
        terrainInformation.topLeftPos = new Vector3(coordinates.x*terrainWidth - terrainWidth/2f, 0 ,coordinates.y*terrainWidth - terrainWidth/2f) + currentOffset;
        terrainInformation.bounds.SetMinMax(terrainInformation.topLeftPos, terrainInformation.topLeftPos + new Vector3(terrainWidth,10,terrainWidth));
        terrain.transform.position= terrainInformation.topLeftPos;

        ResetPostAndDistMap(terrainInformation);
        ImprintNeighborEdges(terrainInformation);
        
        if (Application.isPlaying) {
            StartCoroutine(ThreadedInitialDistanceMapCalculation(terrainInformation,paths,completeCallback));
        } else {
	        CalculateInitialDistanceMaps(terrainInformation, paths);
            DoneInitialDistanceMapCalculation(terrainInformation,completeCallback);
        }
        
        GetComponent<PathAndTerrainGenerator>().myTerrains.Add(terrainInformation);
        
        //stopwatch.Stop();
        //Debug.Log($"Initial Creation Time: {stopwatch.ElapsedMilliseconds}ms");
    }



    IEnumerator ThreadedInitialDistanceMapCalculation(TrainTerrain trainTerrain,List<PathGenerator.TrainPath> paths, Action completeCallback) {
        bool done = false;
        trainTerrain.beingProcessed = true;
        yield return null;
        /*new Thread(()=>{
            CalculateInitialDistanceMaps(trainTerrain, paths);
            done = true;
        }).Start();*/

        ThreadPool.QueueUserWorkItem(o => {
            CalculateInitialDistanceMaps(trainTerrain, paths);
            done = true;
        });
        
        while (!done)
            yield return null;
        trainTerrain.beingProcessed = false;
        DoneInitialDistanceMapCalculation(trainTerrain,completeCallback);
    }

    void CalculateInitialDistanceMaps(TrainTerrain information, List<PathGenerator.TrainPath> paths) {
        //noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        /*var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();*/
        ImprintPaths(information, paths);
        //stopwatch.Stop();
        //Debug.Log($"Path Imprint Time: {stopwatch.ElapsedMilliseconds}ms");

        //stopwatch.Restart();
        CalculateDistanceMap(information);
        
        /*stopwatch.Stop();
        Debug.Log($"Calculate Distance Map time: {stopwatch.ElapsedMilliseconds}ms");*/
	    //CalculateHeightmap(information);
        //GaussianBlur(information);
    }

    [Range(0,1)]
    public float blurStrength = 1f;
    void GaussianBlur(TrainTerrain information) {
        var heightMap = information.heightmap;
        for (int x = 1; x < gridSize-1; x++) {
            for (int y = 1; y < gridSize-1; y++) {
                var total = heightMap[x, y] * 4 +
                            (heightMap[x + 1, y] + heightMap[x - 1, y] + heightMap[x, y + 1] + heightMap[x, y - 1]) * 2 +
                            (heightMap[x + 1, y + 1] + heightMap[x + 1, y - 1] + heightMap[x - 1, y + 1] + heightMap[x - 1, y - 1]);
                var average = total / 16f;
                heightMap[x, y] = Mathf.Lerp(heightMap[x, y], average, blurStrength);
            }
        }
    }

    public float mountainStartHeight = 0.2f;
    public float mountainHeightRampUp = 0.1f;
    public float mountainBaseHeightRampUp = 0.05f;
    public float mountainBaseHeight = 1;

    public float transitionZoneWidthVariance = 0.5f;
    public float transitionZoneWidthFrequency = 0.5f;
    void CalculateHeightmap(TrainTerrain information) {
        var _buffer = continentalnessMap.keys;
        var _preWrapMode = continentalnessMap.preWrapMode;
        var _postWrapMode = continentalnessMap.postWrapMode;
        var heightCurve = new AnimationCurve(_buffer) {
            preWrapMode = _preWrapMode,
            postWrapMode = _postWrapMode
        };
        
        for (int x = 0; x < gridSize; x++) {
            for (int y = 0; y < gridSize; y++) {
                var distance = information.distanceMap[x, y];
                var height = 0f;
                var posX = information.GetPos_X(x, y);
                var posY = information.GetPos_Y(x, y);
                
                /*var transitionBloatInner = 0;
                var transitionBloatOuter = transitionZoneWidth;*/
                
                if (distance < playZoneWidth) {
                    height = GetPlayZoneHeight(posX, information.GetPos_Y(x,y));
                    
                }else if (distance < playZoneWidth + transitionZoneWidth){   
                    var t = Mathf.Clamp01((distance - (playZoneWidth )) / (transitionZoneWidth));
                    height = playZoneHeight + t*baseSideHeightIncrease + Mathf.Lerp(0, GetMountainousHeight(heightCurve, posX, posY)*mountainStartHeight, ParametricBlend(t));
                    
                } else {
                    var adjustedDistance = distance - (playZoneWidth + transitionZoneWidth);
                    adjustedDistance *= mountainHeightRampUp;
                    adjustedDistance += GetNoise(posX*0.5f, posY*0.5f) * 0.1f;
                    adjustedDistance = Mathf.Clamp(adjustedDistance, 0, 1f-mountainStartHeight);
                    adjustedDistance += mountainStartHeight;
                    
                    var adjustedDistance2 = distance - (playZoneWidth + transitionZoneWidth);
                    adjustedDistance2 *= mountainBaseHeightRampUp;
                    adjustedDistance2 = Mathf.Clamp(adjustedDistance2, 0, 1f);
                    height = playZoneHeight+baseSideHeightIncrease+GetMountainousHeight(heightCurve, posX, posY) * adjustedDistance + adjustedDistance2*mountainBaseHeight;
                    //heightmap[x, y] = Mathf.PerlinNoise(x * scale * frequency, y * scale * frequency);
                }

                information.heightmap[x,y] = AbsoluteHeightToTerrainHeight(height);
            }
        }
        
        for (var y = 0; y < detailGridSize; y++)
        {
            for (var x = 0; x < detailGridSize; x++) {
                var distanceX = Mathf.RoundToInt(((float)x / detailGridSize) * gridSize);
                var distanceY = Mathf.RoundToInt(((float)y / detailGridSize) * gridSize);
                var posX = information.GetPos_X(distanceX, distanceY);
                var posY = information.GetPos_Y(distanceX, distanceY);
                var distance = information.distanceMap[distanceX, distanceY];
                
                var incline = InclineAtPos(distanceX, distanceY, information.heightmap);
                //print(incline);
                information.detailmap0[x, y] = GetGrassDensity(posX, posY, grassFrequency0, grassThreshold0, grassMaxDensity0, distance, incline);
                information.detailmap1[x, y] = GetGrassDensity(posX+500, posY+500, grassFrequency1, grassThreshold1, grassMaxDensity1,distance,incline);
                
                //var realPos = information.GetRealPos(distanceX, distanceY);
                //Debug.DrawLine(realPos, realPos+Vector3.up*distance, Color.yellow,10f);
                //Debug.DrawLine(realPos, realPos + Vector3.up * incline*20, incline < maxGrassIncline ? Color.green : Color.red, 10f);
                //Debug.DrawLine(realPos, realPos + Vector3.up * -20, incline > maxGrassIncline ? Color.green : Color.red, 10f);
            }
        }

        System.Random random = new System.Random();
        List<TreeInstance> treeInstances = new List<TreeInstance>();
        var maxRandomOffset = (1f / treeGridSize) / 2f;
        for (int x = 0; x < treeGridSize; x++) {
            for (int y = 0; y < treeGridSize; y++) {
                var distanceX = Mathf.RoundToInt(((float)x / treeGridSize) * gridSize);
                var distanceY = Mathf.RoundToInt(((float)y / treeGridSize) * gridSize);
                var posX = information.GetPos_X(distanceX, distanceY);
                var posY = information.GetPos_Y(distanceX, distanceY);
                var distance = information.distanceMap[distanceX, distanceY];

                if (NextFloat(random) < GetRandomTreeChance(posX, posY, distance)) {
                    TreeInstance treeTemp = new TreeInstance();
                    treeTemp.position = new Vector3((float)x/treeGridSize,0, (float)y/treeGridSize) + new Vector3(NextFloat(random, -maxRandomOffset,maxRandomOffset), 0, NextFloat(random,-maxRandomOffset,maxRandomOffset));
                    treeTemp.prototypeIndex = 0;
                    treeTemp.widthScale = NextFloat(random,0.5f,1.2f);
                    treeTemp.heightScale = treeTemp.widthScale;
                    treeTemp.rotation = NextFloat(random, 0f, 2 * Mathf.PI);
                    treeTemp.color = Color.white;
                    treeTemp.lightmapColor = Color.white;
                    treeInstances.Add(treeTemp);
                }
            }
        }

        information.treeInstances = treeInstances.ToArray();
    }

    static float InclineAtPos(int x, int y, float[,] map) {
        var multiplier = 1000f/((float)gridSize / terrainWidth);
        
        var xmax = Mathf.Clamp(x + 1, 0, gridSize - 1);
        var xmin = Mathf.Clamp(x - 1, 0, gridSize - 1);
        var dx = (map[xmax, y] - map[xmin, y]) / (xmax-xmin);
        
        var ymax = Mathf.Clamp(y + 1, 0, gridSize - 1);
        var ymin = Mathf.Clamp(y - 1, 0, gridSize - 1);
        var dy = (map[x, ymax] - map[x, ymin]) / (ymax-ymin);

        return Mathf.Sqrt(dx * dx + dy * dy)*multiplier;
    }
    static float NextFloat(System.Random random, float min = 0f, float max =1f) {
        double val = (random.NextDouble() * (max - min) + min);
        return (float)val;
    }

    public int treeGridSize = 65;
    public float treeFrequency = 0.1f;
    public float treeThreshold = 0.6f;
    public float treeMaxThreshold = 0.7f;
    public float treeMinDistance = 3;
    public float treeFadeToMax =5;

    float GetRandomTreeChance(float x, float y, float distance) {
        var value = GetNoise(seed.x + x * treeFrequency * scale, seed.y + y * treeFrequency * scale);
        if (value > treeThreshold && distance > treeMinDistance) {
            value = value - treeThreshold;
            value /= (1 - treeThreshold);
            value = Mathf.Clamp01(value);
            if (value > treeMaxThreshold) {
                value = 1;
            }
            var distanceMultiplier = (distance- treeMinDistance)/treeFadeToMax;
            distanceMultiplier = Mathf.Clamp01(distanceMultiplier);

            return value * distanceMultiplier;
        } else {
            return 0;
        }
    }

    public float grassFrequency0 = 1f;
    public float grassFrequency1 = 1f;
    public float grassThreshold0 = 0.5f;
    public float grassThreshold1 = 0.3f;
    public int grassMaxDensity0 = 16;
    public int grassMaxDensity1 = 5;
    public float minGrassDistance = 3;
    public float grassFadeToMaxWidth = 5;
    private float maxGrassIncline = 0.03f;
    private float grassInclineFadeWidth = 0.01f;
    
    float scale = 3 / 4f; // change this if you change the size of the terrain
    private int GetGrassDensity(float x, float y, float frequency, float threshold, float density, float distance, float incline) {
        if (distance > minGrassDistance && incline < maxGrassIncline) {
            var value = GetNoise(seed.x + x * frequency * scale, seed.y + y * frequency * scale);
            if (value > threshold) {
                value = value - threshold;
                value /= (1 - threshold);
                value = Mathf.Clamp01(value);
                value *= density;
                var distanceMultiplier = (distance - minGrassDistance) / grassFadeToMaxWidth;
                distanceMultiplier = Mathf.Clamp01(distanceMultiplier);
                var inclineMultiplier = (maxGrassIncline - incline) / grassInclineFadeWidth;
                inclineMultiplier = Mathf.Clamp01(inclineMultiplier);
                value *= distanceMultiplier;
                value *= inclineMultiplier;
                var result = Mathf.RoundToInt(value);
                return result;
            } 
        }

        return 0;
    }
    
    private float GetPlayZoneHeight(float x, float y) {
        var frequency = 1f;
        return playZoneHeight + ((GetNoise(seed.x + x  * frequency, seed.y + y  * frequency)-0.5f)*playZoneHeightVariance);
    }

    public float mountainFrequency = 0.25f;
    public float mountainFrequencyDeeper = 0.25f;
    private float GetMountainousHeight(AnimationCurve curve, float x, float y) {
        return curve.Evaluate(
            GetNoise(seed.x + x  * mountainFrequency * scale, seed.y + y  * mountainFrequency * scale)/2f + 
            GetNoise(seed.x + x  * mountainFrequencyDeeper * scale, seed.y + y  * mountainFrequencyDeeper*scale)/2f) 
               * maxMountainHeight;
    }

    float AbsoluteHeightToTerrainHeight(float height) {
        return height / terrainHeight;
    }

    float GetNoise(float x, float y) {
        return Mathf.PerlinNoise(x, y);
    }

    void DoneInitialDistanceMapCalculation(TrainTerrain information, Action completeCallback) {
        //DebugSetOtherMap(information, information.distanceMap);
        //SetTerrainData(information);

        if (Application.isPlaying) {
            StartCoroutine(FinishUpTerrain(information));
        } else {
            while (information.needReImprint) {
                if (information.needReDistance) {
                    if (!information.distanceBeingCalculated) {
                        CalculateDistanceMap(information);
                    }
                }
                ImprintNeighborEdges(information);
            }
            
            if (information.needReDraw || information.needReDistance) {
                CalculateChangedDistanceMapsAndHeightMaps(information);
                DoneCalculateChangedDistanceMapsAndHeightMaps(information);
            } else {
                Debug.Log($"No need to finish up {information.coordinates.x}, {information.coordinates.y}");
                information.beingFinished = false;
                information.beingProcessed = false;
            }
        }
        completeCallback?.Invoke();
    }

    IEnumerator FinishUpTerrain(TrainTerrain information) {
        /*if (information.beingFinished) {
            Debug.Log($"Finishing up is blocked for {information.coordinates.x}, {information.coordinates.y}");
        }*/

        while (information.beingFinished) {
            yield return null;
        }
        
        //Debug.Log($"Try to finish up terrain at {information.coordinates.x}, {information.coordinates.y}");
        information.beingFinished = true;
        information.beingProcessed = true;

        while (information.needReImprint) {
            if (information.needReDistance) {
                if (!information.distanceBeingCalculated) {
                    ThreadPool.QueueUserWorkItem(o => {
                        CalculateDistanceMap(information);
                    });
                }

                yield return null;
                continue;
            }
            
            //Debug.Log($"waiting do reimprint {information.coordinates.x}, {information.coordinates.y}");
            ImprintNeighborEdges(information);
            yield return null;
            //yield return new WaitForSeconds(0.05f); // because it takes like 700ms to finish up a terrain. maybe not anymore?
        }
        
        if (information.needReDraw || information.needReDistance) {
            if (Application.isPlaying) {
                StartCoroutine(ThreadedHeightmapCalculation(information));
            } else {
                CalculateChangedDistanceMapsAndHeightMaps(information);
                DoneCalculateChangedDistanceMapsAndHeightMaps(information);
            }
        } else {
            //Debug.Log($"No need to finish up {information.coordinates.x}, {information.coordinates.y}");
            information.beingFinished = false;
            information.beingProcessed = false;
        }
    }

    void FinishUpTerrainNonThreaded(TrainTerrain information) {
        information.beingFinished = true;
        information.beingProcessed = true;
        
        while (information.needReImprint) {
            if (information.needReDistance) {
                if (!information.distanceBeingCalculated) {
                    CalculateDistanceMap(information);
                }
            }
            
            ImprintNeighborEdges(information);
        }
        
        if (information.needReDraw || information.needReDistance) {
            CalculateChangedDistanceMapsAndHeightMaps(information);
            DoneCalculateChangedDistanceMapsAndHeightMaps(information);
        } else {
            //Debug.Log($"No need to finish up {information.coordinates.x}, {information.coordinates.y}");
            information.beingFinished = false;
            information.beingProcessed = false;
        }
    }


    public void SyncTerrainEdgesAndRecalculateHeightmaps(Action completeCallback) {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        
        var pathAndTerrainGen = GetComponent<PathAndTerrainGenerator>();
        for (int i = 0; i < pathAndTerrainGen.myTerrains.Count; i++) {
            var information = pathAndTerrainGen.myTerrains[i];

            var leftNeighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.left );
            var topNeighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.up );
            var rightNeighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.right );
            var bottomNeighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.down );
            
            information.terrain.SetNeighbors(
                leftNeighbor?.terrain, 
                topNeighbor?.terrain,
                rightNeighbor?.terrain,
                bottomNeighbor?.terrain
                );
            
            SyncEdges(information, leftNeighbor, Vector2Int.left);
            SyncEdges(information, topNeighbor, Vector2Int.up);
            SyncEdges(information, rightNeighbor, Vector2Int.right);
            SyncEdges(information, bottomNeighbor, Vector2Int.down);
        }
        
        
        stopwatch.Stop();
        //Debug.Log($"Edge Syncing Time: {stopwatch.ElapsedMilliseconds}ms");
        
        for (int i = 0; i < pathAndTerrainGen.myTerrains.Count; i++) {
            var information = pathAndTerrainGen.myTerrains[i];

            if (information.needReDraw) {
                if (Application.isPlaying) {
                    StartCoroutine(ThreadedHeightmapCalculation(information));
                } else {
                    CalculateChangedDistanceMapsAndHeightMaps(information);
                    DoneCalculateChangedDistanceMapsAndHeightMaps(information);
                }
            }
        }
    }
    
    IEnumerator ThreadedHeightmapCalculation(TrainTerrain trainTerrain) {
        bool done = false;
        yield return null;
        /*new Thread(()=>{
            CalculateChangedDistanceMapsAndHeightMaps(trainTerrain);
            done = true;
        }).Start();
        */
        ThreadPool.QueueUserWorkItem(o => {
            CalculateChangedDistanceMapsAndHeightMaps(trainTerrain);
            done = true;
        });
        
        while (!done)
            yield return null;
        DoneCalculateChangedDistanceMapsAndHeightMaps(trainTerrain);
    }

    void SyncEdges(TrainTerrain mainTerrain, TrainTerrain sideTerrain, Vector2Int direction) {
        if(sideTerrain == null)
            return;

        if (sideTerrain.distanceBeingCalculated || sideTerrain.needReDistance) {
            mainTerrain.needReImprint = true;
            if (!Application.isPlaying) {
                CalculateDistanceMap(sideTerrain);
            }
            return;
        }

        var madeChange = false;
        if (direction == Vector2Int.left && sideTerrain.rightDirty) {
            for (int y = 0; y < gridSize; y++) {
                if (mainTerrain.distanceMap[0, y] > sideTerrain.distanceMap[gridSize - 1, y]) {
                    mainTerrain.distanceMap[0, y] = sideTerrain.distanceMap[gridSize - 1, y];
                    madeChange = true;
                    //Debug.DrawLine(mainTerrain.bounds.center + Vector3.up*20,mainTerrain.GetRealPos(0,y) +Vector3.up*5, Color.blue, 1f );
                    //Debug.DrawLine(sideTerrain.bounds.center ,sideTerrain.GetRealPos(gridSize - 1, y) +Vector3.up*5, Color.blue, 1f );
                }
            }

            sideTerrain.rightDirty = false;

        }else if (direction == Vector2Int.up && sideTerrain.downDirty) {
            for (int x = 0; x < gridSize; x++) {
                if (mainTerrain.distanceMap[x, gridSize-1] > sideTerrain.distanceMap[x, 0]) {
                    mainTerrain.distanceMap[x, gridSize-1] = sideTerrain.distanceMap[x, 0];
                    madeChange = true;
                    //Debug.DrawLine(mainTerrain.bounds.center + Vector3.up*20,mainTerrain.GetRealPos(x, gridSize - 1) +Vector3.up*5, Color.red, 1f );
                    //Debug.DrawLine(sideTerrain.bounds.center ,sideTerrain.GetRealPos(x, 0) +Vector3.up*5, Color.red, 1f );
                }
            }

            sideTerrain.downDirty = false;

        }else if (direction == Vector2Int.right && sideTerrain.leftDirty) {
            for (int y = 0; y < gridSize; y++) {
                if (mainTerrain.distanceMap[gridSize-1, y] > sideTerrain.distanceMap[0, y]) {
                    mainTerrain.distanceMap[gridSize-1, y] = sideTerrain.distanceMap[0, y];
                    madeChange = true;
                    //Debug.DrawLine(mainTerrain.bounds.center + Vector3.up*20,mainTerrain.GetRealPos(gridSize - 1,y) +Vector3.up*5, Color.green, 1f );
                    //Debug.DrawLine(sideTerrain.bounds.center ,sideTerrain.GetRealPos(0, y) +Vector3.up*5, Color.green, 1f );
                }
            }

            sideTerrain.leftDirty = false;

        }else if (direction == Vector2Int.down && sideTerrain.upDirty) {
            for (int x = 0; x < gridSize; x++) {
                if (mainTerrain.distanceMap[x, 0] > sideTerrain.distanceMap[x,gridSize-1]) {
                    mainTerrain.distanceMap[x, 0] = sideTerrain.distanceMap[x,gridSize-1];
                    madeChange = true;
                    //Debug.DrawLine(mainTerrain.bounds.center + Vector3.up*20,mainTerrain.GetRealPos(x, 0) +Vector3.up*5, Color.magenta, 1f );
                    //Debug.DrawLine(sideTerrain.bounds.center ,sideTerrain.GetRealPos(x,gridSize - 1) +Vector3.up*5, Color.magenta, 1f );
                }
            }

            sideTerrain.upDirty = false;
        }

        if (madeChange) {
            mainTerrain.needReDraw = true;
            mainTerrain.needReDistance = true;
            //Debug.Log("Syncing edges made a difference!!!");
        }
    }


    void CalculateChangedDistanceMapsAndHeightMaps(TrainTerrain trainTerrain) {
        //var stopwatch = new System.Diagnostics.Stopwatch();
        //stopwatch.Start();
        CalculateDistanceMap(trainTerrain);
        //stopwatch.Stop();
        //Debug.Log($"Calculate Distance map 2 Time: {stopwatch.ElapsedMilliseconds}ms");
        //stopwatch.Restart();
        CalculateHeightmap(trainTerrain);
        //stopwatch.Stop();
        //Debug.Log($"Calculate Heightmap Time: {stopwatch.ElapsedMilliseconds}ms");
        //stopwatch.Restart();
        GaussianBlur(trainTerrain);
        //stopwatch.Stop();
        //Debug.Log($"Calculate Gaussian Blur Time: {stopwatch.ElapsedMilliseconds}ms");
    }
    
    void DoneCalculateChangedDistanceMapsAndHeightMaps(TrainTerrain information) {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        
        SetTerrainData(information);
        //DebugSetOtherMap(information, information.distanceMap);
        information.needReDraw = false;

        var pathAndTerrainGen = information.pathAndTerrainGenerator;
        if (information.leftDirty) {
            var neighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.left );
            if (neighbor != null) {
                neighbor.needReImprint = true;
                StartCoroutine(FinishUpTerrain(neighbor));
            }
        }
        if (information.rightDirty) {
            var neighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.right );
            if (neighbor != null) {
                neighbor.needReImprint = true;
                StartCoroutine(FinishUpTerrain(neighbor));
            }
        }
        if (information.upDirty) {
            var neighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.up );
            if (neighbor != null) {
                neighbor.needReImprint = true;
                StartCoroutine(FinishUpTerrain(neighbor));
            }
        }
        if (information.downDirty) {
            var neighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.down );
            if (neighbor != null) {
                neighbor.needReImprint = true;
                StartCoroutine(FinishUpTerrain(neighbor));
            }
        }
        

        information.beingFinished = false;
        information.beingProcessed = false;
        stopwatch.Stop();
        //Debug.Log($"Finished up terrain at {information.coordinates.x}, {information.coordinates.y}");
        //Debug.Log($"Set Terrain Data Time: {stopwatch.ElapsedMilliseconds}ms");
    }


    public void ReprocessTerrainChanges(TrainTerrain information) {
        information.needReDraw = false;

        var pathAndTerrainGen = information.pathAndTerrainGenerator;
        if (information.leftDirty) {
            var neighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.left );
            if (neighbor != null) {
                neighbor.needReImprint = true;
                FinishUpTerrainNonThreaded(neighbor);
            }
        }
        if (information.rightDirty) {
            var neighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.right );
            if (neighbor != null) {
                neighbor.needReImprint = true;
                FinishUpTerrainNonThreaded(neighbor);
            }
        }
        if (information.upDirty) {
            var neighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.up );
            if (neighbor != null) {
                neighbor.needReImprint = true;
                FinishUpTerrainNonThreaded(neighbor);
            }
        }
        if (information.downDirty) {
            var neighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.down );
            if (neighbor != null) {
                neighbor.needReImprint = true;
                FinishUpTerrainNonThreaded(neighbor);
            }
        }
        

        information.beingFinished = false;
        information.beingProcessed = false;
    }

    void ResetPostAndDistMap(TrainTerrain trainTerrain) {
        //var positionMap = trainTerrain.positionMap;
        var distanceMap = trainTerrain.distanceMap;
        
        /*var gridStep = ((float)terrainWidth)/(gridSize-1);

        for (int x = 0; x < gridSize; x++) {
            for (int y = 0; y < gridSize; y++) {
                positionMap[x, y] = new Vector3(x*gridStep,0,y*gridStep) + trainTerrain.cornerPosition;
            }
        }*/

        for (int x = 0; x < gridSize; x++) {
            for (int y = 0; y < gridSize; y++) {
                distanceMap[x, y] = maxDistance;
            }
        }
    }

    void ImprintPaths(TrainTerrain trainTerrain, List<PathGenerator.TrainPath> paths) {
        var distanceMap = trainTerrain.distanceMap;
        
        for (int k = 0; k < paths.Count; k++) {
            var trainPath = paths[k];
            var distance = 0f;
            for (int i = 0; i < trainPath.points.Length; i++) {
                AddPointToDistanceMap(trainTerrain, trainPath.points[i], distanceMap);
                var forward = PathGenerator.GetDirectionVectorOnTheLine(trainPath, distance);
                var left = Quaternion.Euler(0,90,0) * forward;
                var right =Quaternion.Euler(0,-90,0) * forward;

                if (trainPath.addImprintNoise) {
                    AddPointToDistanceMap(trainTerrain, trainPath.points[i] + left * (GetNoise(seed.x + distance * transitionZoneWidthFrequency, 0)) * transitionZoneWidthVariance, distanceMap);
                    AddPointToDistanceMap(trainTerrain, trainPath.points[i] + right * (GetNoise(0, seed.y + distance * transitionZoneWidthFrequency)) * transitionZoneWidthVariance, distanceMap);
                }

                distance += trainPath.stepLength;
            }
        }
    }

    void ImprintNeighborEdges(TrainTerrain information) {
        var pathAndTerrainGen = information.pathAndTerrainGenerator;
        
        var leftNeighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates+ Vector2Int.left );
        var topNeighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates+ Vector2Int.up );
        var rightNeighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.right );
        var bottomNeighbor = pathAndTerrainGen.GetTerrainAtCoordinates(information.coordinates + Vector2Int.down );
            
        information.terrain.SetNeighbors(
            leftNeighbor?.terrain, 
            topNeighbor?.terrain,
            rightNeighbor?.terrain,
            bottomNeighbor?.terrain
        );

        information.needReImprint = false;
        SyncEdges(information, leftNeighbor, Vector2Int.left);
        SyncEdges(information, topNeighbor, Vector2Int.up);
        SyncEdges(information, rightNeighbor, Vector2Int.right);
        SyncEdges(information, bottomNeighbor, Vector2Int.down);
    }
    
	void CalculateDistanceMap(TrainTerrain trainTerrain) {
        if(!trainTerrain.needReDistance)
            return;

        trainTerrain.distanceBeingCalculated = true;
        
        var distanceMap = trainTerrain.distanceMap;
	    
        var directDist = 1f;
        var sideDist = 1.41f;
        directDist *= gridStep;
        sideDist *= gridStep;

        for (int x = 1; x < gridSize; x++) {
            for (int y = 1; y < gridSize; y++) {
                var minVal = Mathf.Min(distanceMap[x, y], distanceMap[x - 1, y] + directDist, distanceMap[x, y - 1] + directDist, distanceMap[x - 1, y - 1] + sideDist);
                if (x == gridSize - 1) {
                    if (!trainTerrain.rightDirty) {
                        if (distanceMap[x, y] > minVal) {
                            trainTerrain.rightDirty = true;
                        }
                    }
                } 
                if (y == gridSize - 1) {
                    if (!trainTerrain.upDirty) {
                        if (distanceMap[x, y] > minVal) {
                            trainTerrain.upDirty = true;
                        }
                    }
                } 
                
                distanceMap[x, y] = minVal;
            }
        }

        for (int x = gridSize - 2; x >= 0; x--) {
            for (int y = gridSize - 2; y >= 0; y--) {
                var minVal = Mathf.Min(distanceMap[x, y], distanceMap[x + 1, y] + directDist, distanceMap[x, y + 1] + directDist, distanceMap[x + 1, y + 1] + sideDist);
                if (x == 0) {
                    if (!trainTerrain.leftDirty) {
                        if (distanceMap[x, y] > minVal) {
                            trainTerrain.leftDirty = true;
                        }
                    }
                }

                if (y == 0) {
                    if (!trainTerrain.downDirty) {
                        if (distanceMap[x, y] > minVal) {
                            trainTerrain.downDirty = true;
                        }
                    }
                }

                distanceMap[x, y] = minVal;
            }
        }

        // all the edges
        {
            {
                var x = 0;
                for (int y = 1; y < gridSize; y++) {
                    var minVal = Mathf.Min(distanceMap[x, y], distanceMap[x, y - 1] + directDist);
                    distanceMap[x, y] = minVal;
                }
            }
            {
                var y = 0;
                for (int x = 1; x < gridSize; x++) {
                    var minVal = Mathf.Min(distanceMap[x, y], distanceMap[x-1,y] + directDist);
                    distanceMap[x, y] = minVal;

                }
            }
            {
                var x = gridSize - 1;
                for (int y = gridSize - 2; y >= 0; y--) {
                    var minVal = Mathf.Min(distanceMap[x, y], distanceMap[x, y + 1] + directDist);
                    distanceMap[x, y] = minVal;
                }
            }
            {
                var y = gridSize - 1;
                for (int x = gridSize - 2; x >= 0; x--) {
                    var minVal = Mathf.Min(distanceMap[x, y], distanceMap[x + 1, y] + directDist);
                    distanceMap[x, y] = minVal;
                }
            }
        }
        trainTerrain.needReDistance = false;
        trainTerrain.needReDraw = true;
        trainTerrain.distanceBeingCalculated = false;
    }
    
    [Serializable]
    public struct Pixel {
        public int x;
        public int y;
        public float dist;
    }

    private static void AddPointToDistanceMap(TrainTerrain trainTerrain, Vector3 point, float[,] distanceMap) {
        var localSpace = point - (trainTerrain.topLeftPos);
        var x = Mathf.FloorToInt(localSpace.x / gridStep);
        var y = Mathf.FloorToInt(localSpace.z / gridStep);
        if (x >= 0 && y >= 0 && x < gridSize && y < gridSize) {
            distanceMap[x, y] = 0;
            trainTerrain.needReDistance = true;
        }
    }

    void SetTerrainData(TrainTerrain information) {
        var terrain = information.terrain;
        if (terrain == null || terrain.gameObject == null)
            return;
        var terrainData = terrain.terrainData;

        terrainData.SetHeights(0, 0, Transpose(information.heightmap));

        terrain.terrainData = terrainData;
        terrain.GetComponent<TerrainCollider>().terrainData = terrainData;

        // TERRAIN DETAILS DISABLED HERE
        //information.terrain.terrainData.SetDetailLayer(0, 0, 0, Transpose(information.detailmap0));
        //information.terrain.terrainData.SetDetailLayer(0, 0, 1, Transpose(information.detailmap1));

        information.terrain.terrainData.SetTreeInstances(information.treeInstances, true);
    }


    T[,] Transpose<T>(T[,] arr) {
        var size = arr.GetLength(0);
        T[,] transposed = new T[size, size];
        
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                transposed[y, x] = arr[x, y];
            }
        }

        return transposed;
    }
    
    public void DebugSetOtherMap(TrainTerrain information, float[,] map) {
        var terrain = information.terrain;
        var terrainData = terrain.terrainData;
	    
        float[,] transposed = new float[gridSize, gridSize];
        
        
        for (int x = 0; x < gridSize; x++) {
            for (int y = 0; y < gridSize; y++) {
                transposed[y, x] = map[x, y];
            }
        }
        
        var maxVal = 0f;
        for (int x = 0; x < gridSize; x++) {
            for (int y = 0; y < gridSize; y++) {
                maxVal = Mathf.Max(maxVal, transposed[x, y]);
            }
        }
         
        for (int x = 0; x < gridSize; x++) {
            for (int y = 0; y < gridSize; y++) {
                transposed[x, y] /= maxVal;
            }
        }
        
        
        terrainData.SetHeights(0,0, transposed);
        
        terrain.terrainData = terrainData;
    }

    public void RetryFinishing(TrainTerrain information) {
        StartCoroutine(FinishUpTerrain(information));
    }
    
    float ParametricBlend(float t,float alpha = 2.5f)
    {
        return Mathf.Pow(t, alpha) / (Mathf.Pow(t, alpha) + Mathf.Pow((1-t),alpha));
    }
}


/*[Header("values")]
    protected int slicesX = 1;
    public float innerRoadWidth = 8;
    public float transitionWidth = 1;
    public float outerDetailWidth = 8;
    public int transitionDetail = 4;
    public float edgeBaseUp = 1;
    [Header("Inner details")]
    public float magnitudeInner = 0.2f;
    public float frequencyInner = 1f;
    [Header("Outer details")]
    public float magnitudeOuter = 5f;
    public float frequencyOuter = 0.1f;
    [Header("Transition Details")] 
    public float magnitudeTransition = 2f;
    public float maxMagnitudeTransition = 1.5f;
    public float frequencyTransition = 0.2f;

    [Header("Flatness")] 
    public float flatSectionLength = 10;
    public float flatSectionTransition = 2;

    public bool isLeftSide = true;

    public AnimationCurve continentalnessMap = AnimationCurve.Linear(0,0,1,1);
    public AnimationCurve transitionMap = AnimationCurve.Linear(0,0,1,1);

    [Button]
    public void BuildMeshEditor() {
        RebuildImmediate();
    }
    protected override void BuildMesh() {
        GenerateVertices();
        MeshUtility.GeneratePlaneTriangles(ref _tsMesh.triangles, slicesX, (sampleCount), false);
    }


    enum TransitionState {
        inner, transition, outer
    }
    void GenerateVertices() {
        var yDist = spline.CalculateLength() / sampleCount;
        var transitionDist = transitionWidth / transitionDetail;
        var innerSlices = Mathf.CeilToInt(innerRoadWidth / yDist);
        var outerSlices = Mathf.CeilToInt(outerDetailWidth / yDist);
        slicesX = innerSlices + outerSlices + transitionDetail;
        int vertexCount = (slicesX + 1) * (sampleCount);
        AllocateMesh(vertexCount, slicesX * ((sampleCount) - 1) * 6);
        int vertexIndex = 0;

        ResetUVDistance();

        bool hasOffset = offset != Vector3.zero;
        var currentDistance = 0f;
        GetSample(0, ref evalResult);
        var lastDistance = evalResult.position;
        for (int i = 0; i < (sampleCount); i++) {
            GetSample(i, ref evalResult);
            
            Vector3 center = Vector3.zero;
            try {
                center = evalResult.position;
            } catch (System.Exception ex) {
                Debug.Log(ex.Message + " for i = " + i);
                return;
            }

            currentDistance += Vector3.Distance(lastDistance, center);
            //print(currentDistance);
            lastDistance = center;

            var sideEdgeTransitionMultipler = 0f;
            if (currentDistance >= flatSectionLength) {
                var delta = currentDistance - flatSectionLength;
                if (flatSectionTransition > 0) {
                    sideEdgeTransitionMultipler = delta / flatSectionTransition;
                } else {
                    sideEdgeTransitionMultipler = 1;
                }

                sideEdgeTransitionMultipler = Mathf.Clamp01(sideEdgeTransitionMultipler);
            }

            Vector3 right = evalResult.right;
            float resultSize = GetBaseSize(evalResult);
            if (hasOffset) {
                center += (offset.x * resultSize) * right + (offset.y * resultSize) * evalResult.up + (offset.z * resultSize) * evalResult.forward;
            }

            //float fullSize = size * resultSize;
            float fullSize = (innerSlices*yDist) + (transitionDist*transitionDetail) + (outerSlices*yDist);
            Vector3 lastVertPos = Vector3.zero;
            Quaternion rot = Quaternion.AngleAxis(rotation, evalResult.forward);
            if (uvMode == MeshGenerator.UVMode.UniformClamp || uvMode == MeshGenerator.UVMode.UniformClip) AddUVDistance(i);
            Color vertexColor = GetBaseColor(evalResult) * color;

            var beginVertIndex = vertexIndex;
            var distance = 0f;

            if (!isLeftSide) {
                distance = -fullSize;
            }
            
            
            var transitionBloatInner = transitionMap.Evaluate(Mathf.PerlinNoise(center.x * frequencyTransition, center.z * frequencyTransition))*magnitudeTransition;
            var transitionBloatOuter = transitionMap.Evaluate(Mathf.PerlinNoise(center.z * frequencyTransition, center.x * frequencyTransition))*magnitudeTransition;
            var total = (transitionBloatInner + transitionBloatOuter)/maxMagnitudeTransition;
            if (total > 1) {
                transitionBloatInner *= 1 / total;
                transitionBloatOuter *= 1 / total;
            }
            var realTransitionDist = (transitionDist * transitionDetail + transitionBloatInner + transitionBloatOuter) / transitionDetail;
            var realTransitionTotal = realTransitionDist * transitionDetail;
            var transitionCurrent = 0f;

            var curState = TransitionState.inner;
            var sideEdgeTransitionInnerMultipler = 0f;
            for (int n = 0; n < slicesX +1; n++) {

                if (isLeftSide) {
                    sideEdgeTransitionInnerMultipler = ((float)n-(innerSlices+transitionDetail)) / outerSlices;
                    sideEdgeTransitionInnerMultipler = 1- sideEdgeTransitionInnerMultipler;
                } else {
                    sideEdgeTransitionInnerMultipler = (float)n/outerSlices;
                }

                sideEdgeTransitionInnerMultipler = Mathf.Clamp01(sideEdgeTransitionInnerMultipler);

                if (sideEdgeTransitionMultipler >= 1) {
                    sideEdgeTransitionInnerMultipler = 1;
                }

                var randomInner = 0f;
                var randomOuter = 0f;
                
                var distanceFromTransition = 0f;
                var transitionPercent = 0f;
                
                // find vertex pos. Also do some calcs for later
                if ((isLeftSide && n < innerSlices) || (!isLeftSide && n > slicesX-innerSlices+1)) { // inner path
                    curState = TransitionState.inner;
                    if(n > 0)
                        distance += yDist;

                    if (isLeftSide) {
                        if (n == innerSlices - 1)
                            distance -= transitionBloatInner;
                    } else {
                        if (n == (slicesX - innerSlices + 1 + 1)) {
                            distance -= transitionBloatInner - (realTransitionTotal-transitionCurrent);
                        }
                    }
                } else if(((isLeftSide && n < innerSlices+transitionDetail) || (!isLeftSide && n > slicesX-(innerSlices+transitionDetail)+1))){ //transition
                    curState = TransitionState.transition;
                    //distance += realTransitionDist;
                    if (isLeftSide) {
                        transitionPercent = (((float)n - innerSlices)) / (transitionDetail-1);
                    }else{
                        transitionPercent = 1-(((float)n - (slicesX-(innerSlices+transitionDetail))) / (transitionDetail-1));
                    }

                    var distanceDelta = transitionPercent;
                    if (distanceDelta > 0.5f) {
                        distanceDelta = 1 - distanceDelta;
                    }
                    distanceDelta *= 2;
                    distanceDelta = ParametricBlend(distanceDelta,2);
                    //Debug.Log(distanceDelta);
                    //Debug.Log($"{transitionPercent} - {distanceDelta}");

                    transitionCurrent += realTransitionDist * (distanceDelta + 0.2f);
                    distance +=  realTransitionDist * (distanceDelta+0.2f);
                    transitionPercent = ParametricBlend(transitionPercent, 3);
                } else {  // outer details
                    curState = TransitionState.outer;
                    if(n > 0)
                        distance += yDist;

                    if (!isLeftSide) {
                        if (n == outerSlices +1)
                            distance -= transitionBloatOuter;

                        distanceFromTransition = 1-(float)n/outerSlices;
                    } else {
                        if (n == innerSlices + transitionDetail) {
                            distance -= transitionBloatOuter - (realTransitionTotal-transitionCurrent);
                        }

                        distanceFromTransition = (float)(n-(innerSlices + transitionDetail))/outerSlices ;
                    }

                    distanceFromTransition = distanceFromTransition.Remap(0, 0.5f, 0.2f, 1f);
                }
                distanceFromTransition = Mathf.Clamp(distanceFromTransition,0.25f,1f);
                
                // set vertex pos
                var vertexPos = center - rot * right * (distance) + rot * evalResult.up;

                // calculate random variations
                if (!((isLeftSide && n == 0 ) || (!isLeftSide && n == slicesX))) {
                    randomInner = (Mathf.PerlinNoise(vertexPos.x * frequencyInner, vertexPos.z * frequencyInner) - 0.5f) * magnitudeInner;
                    
                    randomOuter = (Mathf.PerlinNoise(vertexPos.x * frequencyOuter, vertexPos.z * frequencyOuter));
                    randomOuter = continentalnessMap.Evaluate(randomOuter);
                    randomOuter *= magnitudeOuter * distanceFromTransition;
                    randomOuter *= sideEdgeTransitionMultipler;
                }
                
                // apply randomness based on state
                float upAmount;
                switch (curState) {
                    case TransitionState.inner:
                        vertexPos += Vector3.up * randomInner;
                        break;
                    case TransitionState.transition:
                        upAmount = Mathf.Lerp(randomInner, randomOuter, transitionPercent) + (transitionPercent * edgeBaseUp);
                        vertexPos += Vector3.up * (upAmount  * sideEdgeTransitionMultipler*sideEdgeTransitionInnerMultipler);
                        break;
                    case TransitionState.outer:
                        upAmount = randomOuter + edgeBaseUp;
                        vertexPos += Vector3.up * (upAmount  * sideEdgeTransitionMultipler*sideEdgeTransitionInnerMultipler);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (vertexIndex == 0)
                    print(vertexPos);
                
                _tsMesh.vertices[vertexIndex] = vertexPos;


                var slicePercent = distance / fullSize;
                //print($"{outerCount}/{outerSlices} - {transitionCount}/{transitionDetail} - {innerCount}/{innerSlices}");
                
                slicePercent = Mathf.Clamp01(slicePercent);
                CalculateUVs(evalResult.percent, 1f - slicePercent);
                _tsMesh.uv[vertexIndex] = Vector2.one * 0.5f + (Vector2)(Quaternion.AngleAxis(uvRotation + 180f, Vector3.forward) * (Vector2.one * 0.5f - __uvs));
                
                _tsMesh.colors[vertexIndex] = vertexColor;
                vertexIndex++;
            }


            vertexIndex = beginVertIndex;
            lastVertPos = Vector3.zero;
            for (int n = 0; n < slicesX +1; n++) {
                if (slicesX > 1) {
                    if (n < slicesX) {
                        Vector3 nextVertPos = _tsMesh.vertices[vertexIndex+1];
                        Vector3 cross1 = -Vector3.Cross(evalResult.forward, nextVertPos - _tsMesh.vertices[vertexIndex]).normalized;

                        if (n > 0) {
                            Vector3 cross2 = -Vector3.Cross(evalResult.forward, _tsMesh.vertices[vertexIndex] - lastVertPos).normalized;
                            _tsMesh.normals[vertexIndex] = Vector3.Slerp(cross1, cross2, 0.5f);
                        } else _tsMesh.normals[vertexIndex] = cross1;
                    } else _tsMesh.normals[vertexIndex] = -Vector3.Cross(evalResult.forward, _tsMesh.vertices[vertexIndex] - lastVertPos).normalized;
                } else {
                    _tsMesh.normals[vertexIndex] = evalResult.up;
                    if (rotation != 0f) _tsMesh.normals[vertexIndex] = rot * _tsMesh.normals[vertexIndex];
                }

                _tsMesh.colors[vertexIndex] = vertexColor;
                lastVertPos = _tsMesh.vertices[vertexIndex];
                vertexIndex++;
            }
        }
    }
    
    float ParametricBlend(float t,float alpha)
    {
        return Mathf.Pow(t, alpha) / (Mathf.Pow(t, alpha) + Mathf.Pow((1-t),alpha));
    }*/
