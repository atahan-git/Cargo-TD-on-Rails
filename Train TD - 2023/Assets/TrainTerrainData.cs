using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
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
      terrain.terrainData = TerrainDataCloner.Clone(terrain.terrainData);
      data.SetData(terrain);
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
   
   [Button]
   public void DebugDrawDetailMap(int mapIndex = 0, float height = 1) {
      var map = data.detailmap0;
      if (mapIndex == 1) {
         map = data.detailmap1;
      }

      var color = Color.red;
      if (mapIndex == 1) {
         color = Color.green;
      }

      for (int x = 0; x < map.GetLength(0); x++) {
         for (int y = 0; y < map.GetLength(1); y++) {
            if (map[x, y] > 0) {
               var pos = data.GetRealPos(x, y);
               pos.y = 2;
               Debug.DrawLine(pos, pos + Vector3.up*map[x,y]*height, color, 20f);
            }
         }
      }
   }
   
   
   List<GameObject> foreignObjects = new List<GameObject>();
   public void AddForeignObject(GameObject obj) {
      foreignObjects.Add(obj);
      obj.transform.SetParent(transform);
   }

   public void RemoveForeignObject(GameObject obj) {
      foreignObjects.Remove(obj);
   }


   public void _DestroyPooledObject() {
      for (int i = 0; i < foreignObjects.Count; i++) {
         Destroy(foreignObjects[i].gameObject);
      }
      
      foreignObjects.Clear();
   }
}
