using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class TrainTerrainData : MonoBehaviour {
   public TerrainGenerator.TrainTerrain data = new TerrainGenerator.TrainTerrain();
   public bool isInitialized = false;
   
   private void Start() {
      if(!isInitialized)
         InitializeData();
   }


   public void InitializeData() {
      var terrain = GetComponent<Terrain>();
      var gridSize = TerrainGenerator.gridSize;
      var detailGridSize = terrain.terrainData.detailWidth;
      terrain.terrainData = TerrainDataCloner.Clone(terrain.terrainData);
      /*var terrainInformation = new TrainTerrain() {
          coordinates = coordinates,
          bounds =  new Bounds(),
          /*positionMap = new Vector3[gridSize, gridSize],#1#
          distanceMap = new float[gridSize, gridSize],
          heightmap = new float[gridSize, gridSize],
          pathAndTerrainGenerator =  GetComponent<PathAndTerrainGenerator>(),
          terrain = terrain,
          needToBePurged =  false
      };*/
      data.distanceMap = new float[gridSize, gridSize];
      data.heightmap = new float[gridSize, gridSize];
      data.terrain = terrain;
      data.pathAndTerrainGenerator = PathAndTerrainGenerator.s;
      data.detailmap0 = new int[detailGridSize, detailGridSize];
      data.detailmap1 = new int[detailGridSize, detailGridSize];

      isInitialized = true;
   }

   [Button]
   public void DebugPrintDetailMaps() {
      
      var detailWidth = data.terrain.terrainData.detailWidth;
      var map = data.terrain.terrainData.GetDetailLayer(0,0,detailWidth, detailWidth, 0);

      for (int x = 0; x < map.GetLength(0); x++) {
         for (int y = 0; y < map.GetLength(1); y++) {
            if (map[x, y] > 0) {
               print($"{x},{y}: {map[x, y]}");
            }
         }
      }
   }
   
   [Button]
   public void DebugPrintTreePositions() {
      var trees = data.terrain.terrainData.treeInstances;

      for (int i = 0; i < trees.Length; i++) {
         print($"Tree {i} at {trees[i].position}");
      }
   }
   
   [Button]
   public void DebugDrawDistances(float height = 1) {
      var map = data.distanceMap;

      for (int x = 0; x < map.GetLength(0); x++) {
         for (int y = 0; y < map.GetLength(1); y++) {
            if (map[x, y] > 0) {
               var pos = data.GetRealPos(x, y); 
               Debug.DrawLine(pos, pos + Vector3.up*map[x,y]*height, Color.red, 20f);
            }
         }
      }
   }
   
   
   List<GameObject> foreignObjects = new List<GameObject>();
   public void AddForeignObject(GameObject obj) {
      foreignObjects.Add(obj);
      obj.transform.SetParent(transform);
   }


   public void _DestroyPooledObject() {
      for (int i = 0; i < foreignObjects.Count; i++) {
         Destroy(foreignObjects[i].gameObject);
      }
      
      foreignObjects.Clear();
   }
}
