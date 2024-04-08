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
      /*data.detailmap0 = new int[detailGridSize, detailGridSize];
      data.detailmap1 = new int[detailGridSize, detailGridSize];
      */

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
      /*var map = data.detailmap0;
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
      }*/
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

   private void OnDestroy() {
      data.DisposeNativeArray();
   }


   public Mesh grassMesh0;
   public Mesh grassMesh1;
   public Material grassMaterial0;
   public Material grassMaterial1;
   public Matrix4x4[][] batches0;
   public Matrix4x4[][] batches1;
   
   private const int maxInstancesPerBatch = 1023;

   public bool drawGrass = false;
   void Update() {
      if (data.hasAnyGrass) {
         Vector3 terrainSize = data.terrain.terrainData.size;
         var terrainCenter = transform.position + terrainSize / 2f;

         /*if (drawGrass && data.grassPositions0.IsCreated && terrainCenter.magnitude < 40) {
            if (data.needReBatchingForGrass) {
               batches0 = ReBatchData(data.grassPositions0);
               batches1 = ReBatchData(data.grassPositions1);
               data.needReBatchingForGrass = false;
            }


            CopyPositionDataToBatches(data.grassPositions0, batches0);
            CopyPositionDataToBatches(data.grassPositions1, batches1);


            DrawGrassInstanced(batches0, grassMesh0, grassMaterial0);
            DrawGrassInstanced(batches1, grassMesh1, grassMaterial1);
         }*/
      }
   }

   void CopyPositionDataToBatches(NativeArray<Matrix4x4> positions, Matrix4x4[][] batches) {
      for (int i = 0; i < batches.Length; i++) {
         var batch = batches[i];
         int startIndex = i * maxInstancesPerBatch;

         // Ensure that we don't exceed the positions array length
         int length = Mathf.Min(maxInstancesPerBatch, positions.Length - startIndex);

         if (length > 0) {
            // Copy data from positions to the current batch
            positions.Slice(startIndex, length).CopyTo(batch);
         }
      }
   }

   Matrix4x4[][] ReBatchData(NativeArray<Matrix4x4> positions) {
      int totalInstances = positions.Length;
      int numBatches = Mathf.CeilToInt((float)totalInstances / maxInstancesPerBatch);
      var batches = new Matrix4x4[numBatches][];

      for (int batchIndex = 0; batchIndex < numBatches; batchIndex++) {
         int startIndex = batchIndex * maxInstancesPerBatch;
         int numInstancesInBatch = Mathf.Min(maxInstancesPerBatch, totalInstances - startIndex);
         batches[batchIndex] = new Matrix4x4[numInstancesInBatch];
      }
      
      //print($"Grass Batch count {numBatches}");

      return batches;
   }

   

   private void DrawGrassInstanced(Matrix4x4[][] batches, Mesh mesh, Material material) {
      for (int i = 0; i < batches.Length; i++) {
         var batch = batches[i];
         //int startIndex = i * maxInstancesPerBatch;
         
         /*for (int j = 0; j < batch.Length; j++) {
            batch[j] = positions[startIndex + j];
         }*/

         Graphics.DrawMeshInstanced(mesh, 0, material, batch, batch.Length);
      }
   }
}
