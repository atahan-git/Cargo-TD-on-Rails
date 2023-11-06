using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class WorldMapHexGrid : MonoBehaviour {

	private HexCell[] cells;

	//public GameObject grassHex;
	public GameObject mountainHex;

	private HexChunk hexChunk;

	public Transform otherObjectsParent;

	public Vector2Int gridSize = new Vector2Int(40, 40);

	public Vector3 startOffset = new Vector3(40, 0, 0);

	float gridOffset => gridSize.x * HexMetrics.outerRadius * 1.1f * gridScale;

	public float gridScale = 0.8f;

	private int gridCount = 1;
	
	public Biome[] biomes;
	
	[Serializable]
	public class Biome {
		public GameObject groundPrefab;
		public PrefabWithWeights[] sideDecor;
	}

	public void CreateGridsOverAFewFrames(GenericCallback callback) {
		gameObject.SetActive(true);
		StartCoroutine(_CreateGridsOverAFewFrames(callback));
	}

	public GameObject hexChunkPrefab;

	private Biome currentBiome;
	public int biomeOverride = -1;
	IEnumerator _CreateGridsOverAFewFrames(GenericCallback callback) {
		var pauseInterval = 1000;
		var n = 0;
		var totalIntervalsCount = (gridSize.x * gridSize.y * gridCount)/pauseInterval;
		var percentIncrease = (1f / 3f) / totalIntervalsCount;
		if (biomeOverride < 0) {
			var targetBiome = DataSaver.s.GetCurrentSave().currentRun.map.GetPlayerStar().currentAct-1;
			if (targetBiome < 0 || targetBiome > biomes.Length) {
				Debug.LogError($"Illegal biome {targetBiome}");
				targetBiome = 0;
			}

			currentBiome = biomes[targetBiome];
		} else {
			currentBiome = biomes[biomeOverride];
		}
		
		for (int i = 0; i < gridCount; i++) {
			hexChunk = Instantiate(hexChunkPrefab, transform).GetComponent<HexChunk>();
			hexChunk.ClearForeign();
			hexChunk.Initialize(gridScale);
			for (int x = - (gridSize.x/2); x < gridSize.x/2; x++) {
				for (int z = - (gridSize.y/2); z < gridSize.y/2; z++) {
				
					var hex = Instantiate(currentBiome.groundPrefab, hexChunk.transform);
					var y = 0f;
					var pos = HexCoordinates.HexToPosition(HexCoordinates.OrdinalToHex(new Vector2Int(x, z)), y);
					hex.transform.localPosition = pos * gridScale + startOffset;
					hex.transform.localScale = Vector3.one*gridScale;
					
					n++;
					
					if (n % pauseInterval == 0) {
						WorldMapCreator.s.worldMapGenerationProgress += percentIncrease;
						yield return null;
					}
				}
			}
			
			hexChunk.transform.position = Vector3.forward * i * (gridOffset) + transform.position;
		}

		yield return null;
		callback?.Invoke();
	}

	public void ClearGrids() {
		StopAllCoroutines();
		
		if (hexChunk != null) {
			Destroy(hexChunk.gameObject);
		}
		hexChunk = null;
	}
	

	public float mountainThreshold = 1.37f;
	//public bool refreshHeightAdjustment = true;
	public GenericCallback myCallback;
	[Button] 
	public void ApplyHeights(GenericCallback _myCallback) {
		StartCoroutine(ApplyHeightsOverAFewFrames());
		//ApplyHeightsOverAFewFrames();
		myCallback = _myCallback;
	}

	struct AffectorStuff {
		public Vector3 posNoY;
		public float posY;
		public float pinDistance;
		public float pinWeight;
		//public AnimationCurve pinDropOff;
		public HexHeightAffector.PinDropOffType type;
		public float randomness;
	}
	IEnumerator ApplyHeightsOverAFewFrames( ) {
		var startTime = Time.realtimeSinceStartup;
		
		cells = GetComponentsInChildren<HexCell>();
		var heightAffectors = GetComponentsInChildren<HexHeightAffector>();
		heightAffectors.Reverse();

		var n = heightAffectors.Length;
		
		// pre-process and put into array heightAffectorStuff
		var affectorStuff = new AffectorStuff[heightAffectors.Length];
		for (int i = 0; i < heightAffectors.Length; i++) {
			var affector = heightAffectors[i];
			var affectorPos = affector.transform.position;
			var posNoY = affectorPos;
			posNoY.y = 0;
			affectorStuff[i] = new AffectorStuff() {
				posNoY = posNoY,
				posY = affectorPos.y,
				pinDistance = affector.pinDistance,
				pinWeight = affector.pinWeight,
				//pinDropOff = affector.pinDropOff,
				type =  affector.myType,
				randomness = affector.randomness,
			};
		}
		
		//refreshHeightAdjustment = true;

		var pauseInterval = cells.Length / 5;
		
		var totalIntervalsCount = (cells.Length)/pauseInterval;
		var percentIncrease = (1f / 3f) / totalIntervalsCount;
		for (int i = 0; i < cells.Length; i++) {
			var currentCell = cells[i];
			var y = 0f;
			
			
			var currentCellTransform = currentCell.transform;

			for (int j = 0; j < heightAffectors.Length; j++) {
				/*var affector = heightAffectors[j];
				var affectorPos = affector.transform.position;
				var affectorPosNoY = affectorPos;
				affectorPosNoY.y = 0;*/
				var affector = affectorStuff[j];
				var cellPosNoY = currentCellTransform.position;
				cellPosNoY.y = 0;
				var distance = Vector3.Distance(cellPosNoY, affector.posNoY);
				var percent = distance / affector.pinDistance;
				percent = Mathf.Clamp(percent,0, 1);
				//var falloff = affector.pinDropOff.Evaluate(percent);
				var falloff = 1f - percent;
				//Debug.Log($"eval: {falloff}, linear: {1f-percent}");
				if (affector.type == HexHeightAffector.PinDropOffType.castle) {
					if (falloff > 0.5f) {
						falloff = 1.2f;
					} else {
						falloff = falloff.Remap(0.5f, 0, 1f, 0f);
					}
				}
				
				var weighted = falloff * affector.pinWeight;
				var heightAdjustment = weighted * affector.posY;
				//var withRandom = heightAdjustment;
				var withRandom = heightAdjustment * Random.Range(1f - affector.randomness, 1f + affector.randomness);
				/*if (refreshHeightAdjustment) {
					withRandom = heightAdjustment * Random.Range(1 - affector.randomness, 1 + affector.randomness);
					currentCell.randomHeightAdjustment = withRandom - heightAdjustment;
				} else {
					withRandom += currentCell.randomHeightAdjustment;
				}*/

				y += withRandom;
			}

			var finalPos = currentCellTransform.localPosition;
			finalPos.y = y;
			//print(y);
			currentCellTransform.localPosition = finalPos;
			
			/*var lerp = (currentCell.transform.position.z - colorLerpStartZ)/(colorLerpEndZ - colorLerpStartZ);
			lerp = Mathf.Clamp01(lerp);
			var propBlock = new MaterialPropertyBlock();
			var renderer = currentCell.GetComponentInChildren<Renderer>();
			renderer.GetPropertyBlock(propBlock);
			propBlock.SetColor("_Color", Color.Lerp(regularColor, crystalColor, lerp));
			renderer.SetPropertyBlock(propBlock);*/
			

			if (finalPos.y > mountainThreshold) {
				if (currentCell.myType != HexCell.HexType.mountain) {
					var hex = Instantiate(mountainHex, hexChunk.transform);
					var hexTransform = hex.transform;
					hexTransform.localPosition = finalPos;
					hexTransform.localScale = Vector3.one*gridScale;
					hexChunk.AddCell(hex);
					Destroy(currentCell.gameObject);
					currentCell = hex.GetComponent<HexCell>();
				} else {
					hexChunk.AddCell(currentCell.gameObject);
				}
			} else {
				if (currentCell.myType != HexCell.HexType.grass) {
					var hex = Instantiate(currentBiome.groundPrefab, hexChunk.transform);
					var hexTransform = hex.transform;
					hexTransform.localPosition = finalPos;
					hexTransform.localScale = Vector3.one*gridScale;
					hexChunk.AddCell(hex);
					Destroy(currentCell.gameObject);
					currentCell = hex.GetComponent<HexCell>();
				}else {
					hexChunk.AddCell(currentCell.gameObject);
				}
			}

			//currentCell.gameObject.isStatic = true;
			
			if (i % pauseInterval == 0) {
				WorldMapCreator.s.worldMapGenerationProgress += percentIncrease;
				yield return null;
			}
		}
		
		//refreshHeightAdjustment = false;

		yield return null;
		//Invoke(nameof(CallbackAfterOneFrame), 0.01f);
		//Debug.Break();
		myCallback?.Invoke();
		//doCallback = true;
		
		
		Debug.Log($"Height Map generated in: {Time.realtimeSinceStartup-startTime}");
	}

	/*public bool doCallback = false;
	private void Update() {
		if (doCallback) {
			myCallback?.Invoke();
			doCallback = false;
		}
	}*/

	/*void CallbackAfterOneFrame() {
		myCallback?.Invoke();
	}*/


	public float railStartGap = 0.2f;
	public float railEndGap = 0.5f;

	public LayerMask railMask;
	
	[Button]
	public void ApplyRails() {
		cells = GetComponentsInChildren<HexCell>();
		var railAffectors = GetComponentsInChildren<HexRailAffector>();

		
		// we assume there is only one chunk
		var segmentLength = HexMetrics.innerRadius * gridScale * (1/railDensityMultiplier);
		
		for (int i = 0; i < railAffectors.Length; i++) {
			var affector = railAffectors[i];
			var trainRailsHex = affector.myPrefab;
			/*var hexCoordStart = hexChunk.GetCellCoords(affector.startPos);
			var hexCoordEnd = hexChunk.GetCellCoords(affector.endPos);
			var targetCells = HexMetrics.cube_linedraw(hexCoordStart, hexCoordEnd);*/
			/*var startNoY = affector.startPos;
			var endNoY = affector.endPos;
			startNoY.y = 0;
			endNoY.y = 0;
			var rotation = Quaternion.LookRotation(startNoY - endNoY);*/
			
			
			var start = affector.startPos;
			var end = affector.endPos;
			start = Vector3.MoveTowards(start, end, railStartGap);
			end = Vector3.MoveTowards(end,start, railEndGap);

			var lastRayResult = new Vector3();

			var ray = new Ray(start+ Vector3.up * 100, Vector3.down);

			if (Physics.Raycast(ray, out RaycastHit hit, 200, railMask)) {
				lastRayResult = hit.point;
			}

			var dist = Mathf.CeilToInt(Vector3.Distance(start, end)/segmentLength);

			for (int j = 1; j < dist+1; j++) {
				var newRayResult = Vector3.zero;
				
				var newRay = new Ray(Vector3.Lerp(start,end, (float)j/dist) + Vector3.up * 100, Vector3.down);

				if (Physics.Raycast(newRay, out RaycastHit newHit, 200, railMask)) {
					newRayResult = newHit.point;
					//Debug.Log(newHit.point);
					//Debug.Log(newHit.collider.gameObject.name);
				}

				if (j == (dist + 1) / 2) {
					affector.transform.position = Vector3.Lerp(lastRayResult, newRayResult, 0.5f);
				}
				
				var rail = Instantiate(trainRailsHex, otherObjectsParent);
				
				rail.transform.position = Vector3.Lerp(lastRayResult, newRayResult, 0.5f);
				var lookVector = lastRayResult - newRayResult;
				if(lookVector.sqrMagnitude > 0)
					rail.transform.rotation = Quaternion.LookRotation(lastRayResult - newRayResult);
				rail.transform.localScale = Vector3.one*gridScale;

				lastRayResult = newRayResult;
			}
		}
	}

	public float railDensityMultiplier = 1.2f;
	List<Vector3> LineDraw(Vector3 start, Vector3 end, float segmentLength) {
		start = Vector3.MoveTowards(start, end, railStartGap);
		end = Vector3.MoveTowards(end,start, railEndGap);

		var dist = Mathf.CeilToInt(Vector3.Distance(start, end)/segmentLength);

		var results = new List<Vector3>();
		
		for (int i = 0; i < dist+1; i++) {
			results.Add(Vector3.Lerp(start,end, (float)i/dist));
		}

		return results;
	}

	public void MeshCombine() {
		AddDetails();
		hexChunk.FinalizeBatches();
		hexChunk.adjustPosition = false;
		
		Invoke(nameof(MakeProbe),0.05f);
	}
	
	void MakeProbe() {
		GetComponentInChildren<ReflectionProbe>().RenderProbe();
	}


	public float decorSpread = 0.2f;
	public float decorYAdjustment = 0;
	void AddDetails() {
		var sideDecorGuides = new GameObject[currentBiome.sideDecor.Length];
		for (int i = 0; i < currentBiome.sideDecor.Length; i++) {
			sideDecorGuides[i] = Instantiate(currentBiome.sideDecor[i].prefab, hexChunk.transform);
		}


		cells = GetComponentsInChildren<HexCell>();
		for (int m = 0; m <cells.Length; m++) {
			if(cells[m].myType == HexCell.HexType.mountain)
				continue;

			var pos = cells[m].transform.localPosition;
			
			// add decors
			var decors = sideDecorGuides;
			var decorCount = Random.Range(0, 2);

			if (decors.Length == 0)
				decorCount = 0;

			for (int i = 0; i < decorCount; i++) {
				var curDecorIndex = PrefabWithWeights.WeightedRandomRoll(currentBiome.sideDecor);
				var curDecor = decors[curDecorIndex];
				var randomOffset = Random.insideUnitCircle * decorSpread;
				curDecor.transform.localPosition = pos + new Vector3(randomOffset.x, decorYAdjustment, randomOffset.y);
				if (currentBiome.sideDecor[curDecorIndex].allRotation) {
					curDecor.transform.rotation = Random.rotation;
				} else {
					curDecor.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
				}
				curDecor.transform.localScale = Vector3.one * gridScale;
				hexChunk.AddCell(curDecor);
			}
		}

		for (int i = 0; i < sideDecorGuides.Length; i++) {
			Destroy(sideDecorGuides[i]);
		}
	}
	
	
	[Button]
	public void DebugGenerateWorldMap() {
		WorldMapCreator.s.DebugGenerateWorldMap();
	}
}