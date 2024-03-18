using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class HexGrid : MonoBehaviour {
	/*public static HexGrid s;

	private void Awake() {
		s = this;
	}

	public int flatDistance = 4;
	public float terrainRandomnessMagnitude = 1f;
	public float terrainSlope = 1f;

	public GameObject hexChunkPrefab;
	public GameObject railsPrefab;
	
	public Biome[] biomes;
	
	[System.Serializable]
	public class Biome {
		public GameObject starterAreaPrefab;
		public GameObject cityPrefab;
		public GameObject groundPrefab;
		public GameObject groundTrackSwitchPrefab;
		public PrefabWithWeights[] sideDecor;
		public Light sun;
		public SkyboxParametersScriptable skybox;
	}

	//public List<Transform> hexParents = new List<Transform>();

	public Vector2Int gridSize = new Vector2Int(40, 40);

	public float gridOffset {
		get {
			return gridSize.x;
			//return gridSize.x * HexMetrics.outerRadius * 1.1f;
		}
	}

	/*public float decorSpread = 1f;

	public int biomeOverride = -1;
	public void CreateCells(Transform hexParent) {
		var hexChunk = hexParent.GetComponent<HexChunk>();
		hexChunk.Initialize();

		if (!DataSaver.s.GetCurrentSave().isInARun)
			biomeOverride = 0;

		Biome currentBiome;
		if (biomeOverride < 0) {
			var targetBiome = DataSaver.s.GetCurrentSave().currentRun.map.GetPlayerStar().biome;
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

		var guideObj = Instantiate(currentBiome.groundPrefab, hexParent);

		var sideDecorGuides = new GameObject[currentBiome.sideDecor.Length];
		for (int i = 0; i < currentBiome.sideDecor.Length; i++) {
			sideDecorGuides[i] = Instantiate(currentBiome.sideDecor[i].prefab, hexParent);
		}


		for (int x = - (gridSize.x/2); x < gridSize.x/2; x++) {
			for (int z = - (gridSize.y/2); z < gridSize.y/2; z++) {
				
				//var hex = Instantiate(hexPrefab, hexParent);
				
				var y = 0f;
				var isPath = Mathf.Abs(z) <= flatDistance;
				if (!isPath) {
					y = (Mathf.Abs(z) - flatDistance) * terrainSlope;
					y += Random.Range(0, Mathf.Abs(z * terrainRandomnessMagnitude));
				}
				
				var pos = HexCoordinates.HexToPosition(HexCoordinates.OrdinalToHex(new Vector2Int(x, z)), y);
				/*var hex = hexChunk.GetCell( x+(gridSize.x/2),z+(gridSize.x/2));
				hex.transform.localPosition = pos;#2#

				guideObj.transform.localPosition = pos;
				
				hexChunk.AddCell(guideObj);

				
				// add decors
				var decors = sideDecorGuides;
				var decorCount = 0;
				if (!isPath) {
					decorCount = Random.Range(0, 5);
				}

				if (decors.Length == 0)
					decorCount = 0;

				for (int i = 0; i < decorCount; i++) {
					var curDecorIndex = PrefabWithWeights.WeightedRandomRoll(currentBiome.sideDecor);
					var curDecor = decors[curDecorIndex];
					var randomOffset = Random.insideUnitCircle * decorSpread;
					curDecor.transform.localPosition = pos + new Vector3(randomOffset.x, 0, randomOffset.y);
					if (currentBiome.sideDecor[curDecorIndex].allRotation) {
						curDecor.transform.rotation = Random.rotation;
					} else {
						curDecor.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
					}

					//Instantiate(curDecor);
					hexChunk.AddCell(curDecor);
				}
				
				

				if (z == 0) {
					var railPos = HexCoordinates.HexToPosition(HexCoordinates.OrdinalToHex(new Vector2Int(x, z)), 0);
					var rails = Instantiate(railsPrefab, hexParent);
					rails.transform.localPosition = railPos;
				}
			}
		}
		
		Destroy(guideObj);
		for (int i = 0; i < sideDecorGuides.Length; i++) {
			Destroy(sideDecorGuides[i]);
		}
		
		hexChunk.FinalizeBatches();
		//UpdateGrid(hexParent);
	}#1#

	/*public void UpdateGrid(Transform hexParent) {
		var hexChunk = hexParent.gameObject.GetComponent<HexChunk>();
		hexChunk.ClearForeign();
		for (int x = - ((int)gridSize.x/2); x < gridSize.x/2; x++) {
			for (int z = - ((int)gridSize.y/2); z < gridSize.y/2; z++) {
				var y = 0f;
				if (Mathf.Abs(z) > flatDistance) {
					y = (z - flatDistance) * terrainSlope;
					y += Random.Range(0, Mathf.Abs(z * terrainRandomnessMagnitude));
				}
				
				var pos = HexCoordinates.HexToPosition(HexCoordinates.OrdinalToHex(new Vector2Int(x, z)), y);
				var hex = hexChunk.GetCell( x+(gridSize.x/2),z+(gridSize.x/2));
				hex.transform.localPosition = pos;
			}
		}
	}#1#


	private void Start() {
		RefreshGrid();
	}
	public int biomeOverride = -1;
	[Button]
	void RefreshGridDebugRUNTIMEONLY(int biome) {
		biomeOverride = biome;
		RefreshGrid();
	}
	public void RefreshGrid() {
		ClearGrids();
		CreateFirstChunk();
		Invoke(nameof(MakeProbe), 0.01f);
	}

	void MakeProbe() {
		transform.parent.GetComponentInChildren<ReflectionProbe>().RenderProbe();
	}

	public Vector2 zRangesToFill;
	void CreateFirstChunk() {
		var currentBiome = GetCurrentBiome();
		
		//gridCount = Mathf.FloorToInt(Mathf.Abs(zRangesToFill.x - zRangesToFill.y) / gridSize.x);
		
		//for (int i = 0; i < gridCount; i++) {
			var hex = Instantiate(currentBiome.cityPrefab, transform);
			//CreateCells(hex.transform);
			hex.transform.position = transform.position - (Vector3.forward* currentBiome.cityPrefab.GetComponent<HexTrackSegment>().myLength/2);
			trackSegments.Add(hex.GetComponent<TrackSwitchHex>());
		//}
		print("made first chunk");
	}

	public void CreateEndAreaChunk() {
		var prefab = GetCurrentBiome().groundPrefab;
		var parentSegment = trackSegments[0].segmentA;

		while (parentSegment.myLength < 120) {
			parentSegment.AttachSegment(Instantiate(prefab).GetComponent<HexTrackSegment>());
		}

		parentSegment.transform.position += Vector3.forward* (SpeedController.s.missionDistance-40);
		print("Made end area chunk");
	}

	Biome GetCurrentBiome() {
		var currentSave = DataSaver.s.GetCurrentSave();
		if (!currentSave.isInARun || !currentSave.isRealSaveFile || currentSave.currentRun.map.GetPlayerStar() == null)
			biomeOverride = 0;

		Biome currentBiome;
		if (biomeOverride < 0) {
			var targetBiome = currentSave.currentRun.map.GetPlayerStar().currentAct-1;
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

		return currentBiome;
	} 

	[ReadOnly]
	public int gridCount;

	void ClearGrids() {
		/*var count = hexParents.Count;
		for (int i =  count-1; i >= 0; i--) {
			var obj = hexParents[i].gameObject;
			Destroy(obj);
		}
		hexParents.Clear();#1#
		/*var count = trackSegments.Count;
		for (int i =  count-1; i >= 0; i--) {
			var obj = trackSegments[i].gameObject;
			Destroy(obj);
		}#1#
		trackSegments.Clear();
		transform.DeleteAllChildren();
	}
	
	/*void ClearGridsEditor() {
		var count = hexParents.Count;
		for (int i =  count-1; i >= 0; i--) {
			var obj = hexParents[i].gameObject;
			DestroyImmediate(obj);
		}
		
		hexParents.Clear();
	}#1#

	public float lastRealDistance = 0;
	//public float distance = 0;
	public List<float> trackSwitchDistances = new List<float>();

	private TrackSwitchHex toAttachTo;
	private bool doubleNextOne = false;
	/*private void Update() {
		var delta = SpeedController.s.currentDistance - lastRealDistance;
		lastRealDistance = SpeedController.s.currentDistance;
		//distance += delta;

		foreach (var parent in hexParents) {
			//parent.GetComponent<Rigidbody>().MovePosition(parent.position + Vector3.back * GlobalReferences.s.speed * Time.deltaTime);
			
			// correct one
			parent.transform.position += Vector3.back * delta;
		}
		
		while (hexParents[0].position.z < zRangesToFill.x) {
			//distance -= gridOffset;
			var lastHex = hexParents[0];
			hexParents.RemoveAt(0);
			foreach (var hexChunk in lastHex.GetComponentsInChildren<HexChunk>()) {
				hexChunk.ClearForeign();
			}
			//lastHex.GetComponent<HexChunk>().ClearForeign();
			//UpdateGrid(lastHex);
			
			lastHex.position = hexParents[hexParents.Count - 1].transform.position + Vector3.forward * gridOffset;
			if (doubleNextOne) {
				lastHex.position = hexParents[hexParents.Count - 1].transform.position + Vector3.forward * (gridOffset * 2);
				doubleNextOne = false;
			}


			if (lastHex.GetComponent <TrackSwitchHex>()) {
				var regularHex = Instantiate(currentBiome.groundPrefab, transform);
				var pos = lastHex.transform.position;
				pos.x = 0;
				regularHex.transform.position = pos;
				hexParents.Insert(0, lastHex.GetComponent<TrackSwitchHex>().attachedHex);
				lastHex.GetComponent<TrackSwitchHex>().attachedHex.SetParent(transform);
				hexParents[0].transform.position = hexParents[1].transform.position - (Vector3.forward * gridOffset);
				hexParents[0].transform.rotation = Quaternion.identity;
				Destroy(lastHex.gameObject);
				lastHex = regularHex.transform;
			}
			
			
			for (int i = 0; i < trackSwitchDistances.Count; i++) {
				if (SpeedController.s.currentDistance + lastHex.position.z > trackSwitchDistances[i]) {
					var trackSwitchHex = Instantiate(currentBiome.groundTrackSwitchPrefab, transform);
					trackSwitchHex.transform.position = lastHex.transform.position;
					trackSwitchHex.GetComponent<TrackSwitchHex>().SetUp();
					Destroy(lastHex.gameObject);
					lastHex = trackSwitchHex.transform;
					
					trackSwitchDistances.RemoveAt(i);

					toAttachTo = lastHex.GetComponent<TrackSwitchHex>();
					hexParents.Add(lastHex);
					return;
				}
			}
			
			if (toAttachTo != null) {
				var fakeHex = Instantiate(lastHex.gameObject).transform;
				toAttachTo.AttachHex(lastHex, fakeHex);
				toAttachTo = null;
				doubleNextOne = true;
			} else {
				hexParents.Add(lastHex);
			}
		}
		while (hexParents[hexParents.Count - 1].position.z > zRangesToFill.y) {
			//distance += gridOffset;
			var lastHex = hexParents[hexParents.Count-1];
			hexParents.RemoveAt(hexParents.Count-1);
			lastHex.GetComponent<HexChunk>().ClearForeign();
			//UpdateGrid(lastHex);
			lastHex.position = hexParents[0].transform.position - Vector3.forward * gridOffset;
			hexParents.Insert(0, lastHex);
		}

	}#1#

	public List<TrackSwitchHex> trackSegments = new List<TrackSwitchHex>();
	private void Update() {
		if(PlayStateMaster.s.isShop())
			return;
		var delta = SpeedController.s.currentDistance - lastRealDistance;
		lastRealDistance = SpeedController.s.currentDistance;
		if (trackSegments.Count > 0) {
			trackSegments[0].transform.position += Vector3.back*delta;
		}
	}


	public void ClearTrackSwitchDistances() {
		trackSwitchDistances.Clear();
	}
	[Button]
	public void DoTrackSwitchAtDistance(float trackSwitchDistance) {
		trackSwitchDistances.Add(trackSwitchDistance-(gridSize.x/2));
	}

	[Button]
	public void AddTrackSwitch(bool lastSelectedSide, float segmentADistance, float segmentBDistance, bool isLastSegment, bool isGoingLeft) {
		StartCoroutine(_AddTrackSwitch(lastSelectedSide, segmentADistance, segmentBDistance, isLastSegment, isGoingLeft));
	}

	IEnumerator _AddTrackSwitch(bool lastSelectedSide, float segmentADistance, float segmentBDistance, bool isLastSegment, bool isGoingLeft) {
		while (makingFirstTrackLock) {
			yield return null;
		}

		var lastSegmentPadding = isLastSegment ? 120 : 0;
		
		var prefab = GetCurrentBiome().groundPrefab;
		var switchHex = Instantiate(GetCurrentBiome().groundTrackSwitchPrefab).GetComponent<TrackSwitchHex>();
		switchHex.isGoingLeft = isGoingLeft;

		var prevSwitch = trackSegments[trackSegments.Count - 1];
		var parentSegment = lastSelectedSide ? prevSwitch.segmentA : prevSwitch.segmentB;

		parentSegment.AttachSwitch(switchHex);

		while (switchHex.segmentA.myLength < (segmentADistance-(switchHex.trackSwitchLength)) + lastSegmentPadding) {
			switchHex.segmentA.AttachSegment(Instantiate(prefab).GetComponent<HexTrackSegment>());
			yield return null;
		}
		
		while (switchHex.segmentB.myLength < (segmentBDistance-(switchHex.trackSwitchLength)) + lastSegmentPadding) {
			switchHex.segmentB.AttachSegment(Instantiate(prefab).GetComponent<HexTrackSegment>());
			yield return null;
		}

		//switchHex.transform.SetParent(transform);
		trackSegments.Add(switchHex);
		if (trackSegments.Count > 3) {
			var toBeDeleted = trackSegments[0];
			trackSegments.RemoveAt(0);
			trackSegments[0].transform.SetParent(transform);
			Destroy(toBeDeleted.gameObject);
		}
	}

	[Button]
	public void MakeFirstPath(float distance) {
		StartCoroutine(_MakeFirstPath(distance));
	}


	private bool makingFirstTrackLock = false;
	IEnumerator _MakeFirstPath(float distance) {
		makingFirstTrackLock = true;
		var prefab = GetCurrentBiome().groundPrefab;
		var parentSegment = trackSegments[0].segmentA;

		while (parentSegment.myLength < distance) {
			parentSegment.AttachSegment(Instantiate(prefab).GetComponent<HexTrackSegment>());
			yield return null;
		}

		makingFirstTrackLock = false;
	}

	public void ResetDistance() {
		lastRealDistance = SpeedController.s.currentDistance;
	}*/
}


[System.Serializable]
public class PrefabWithWeights {
	[HorizontalGroup(LabelWidth = 50)]
	public GameObject prefab;
	[HorizontalGroup(LabelWidth = 20, Width = 100)]
	public float weight = 1f;
	[HorizontalGroup(LabelWidth = 20, Width = 80)]
	public bool allRotation = false;
	
	
	public static int WeightedRandomRoll(PrefabWithWeights[] F) {
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

		return index;
	}
}


[System.Serializable]
public class NumberWithWeights {
	[HorizontalGroup(LabelWidth = 50)]
	public int number;
	[HorizontalGroup(LabelWidth = 20, Width = 100)]
	public float weight = 1f;


	public NumberWithWeights(){}
	
	public NumberWithWeights(int _number, float _weight) {
		number = _number;
		weight = _weight;
	}
	
	public static int WeightedRandomRoll(NumberWithWeights[] F) {
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

		return F[index].number;
	}
}