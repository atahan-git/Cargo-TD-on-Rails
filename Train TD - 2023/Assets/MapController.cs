using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapController : MonoBehaviour {
	public static MapController s;

	private void Awake() {
		s = this;
	}

	public int firstLevelDistance = 1;
	private int bossDistance = 5;

	private int minStarCount = 8;
	private int maxStarCount = 15;

    public int currentAct;
    
    public ActData[] acts;

    [Serializable]
    public class ActData {
	    [Title("Act data")]
	    public LevelArchetypeScriptable[] firstLevels;
	    public LevelArchetypeScriptable[] remainingLevels;
	    public LevelArchetypeScriptable[] bossLevels;
	    
	    [Space]

	    public CityDataScriptable[] firstCities;
	    public CityDataScriptable[] remainingCities;
	    public CityDataScriptable[] bossCities;
    }

    public MenuToggle mapUI;


    //public static bool ApplyStarMap = true;

    private void Start() {
	    //GenerateStarMap();
	    //targetStarInfoScreen.Hide();
	    //selectedStarInfoScreen.Hide();
	    /*if(ApplyStarMap)
			ApplyStarMapFromSave();*/
    }


    [Button]
    public void GenerateStarMap() {
	    Cleanup();
	    var myMap = new List<MapChunk>();

	    currentAct = DataSaver.s.GetCurrentSave().currentRun.currentAct;
	    
	    GenerateMapWithWaveCollapse(myMap);
	    
	    var starMapState = GenerateStarMapStateFromMap(myMap);
	    ConnectAllStars(starMapState);
	    
	    PutPlayerInStar(starMapState.chunks[0].myStars[0]);
	    
	    //var shops = MakeShops(starMapState);
	    MakeBoss(starMapState);

	    DataSaver.s.GetCurrentSave().currentRun.map = starMapState;
	    Debug.Log($"Star map instantiation complete");
	    DataSaver.s.SaveActiveGame();

	    //ApplyStarMapFromSave(true);
    }

    ActData GetCurrentAct() {
	    return acts[currentAct-1];
    }
    

    /*public void ApplyStarMapFromSave(bool force = false) {
	    /*if(DataSaver.s.GetCurrentSave().isInARun || force)
			SpawnStarsAndConnections(DataSaver.s.GetCurrentSave().currentRun.map);#1#
	    
	    //WORLDMAP
	    if(DataSaver.s.GetCurrentSave().isInARun || force)
			WorldMapCreator.s.GenerateWorldMap();
	    
	    //targetStarInfoScreen.Hide();
    }*/

    private StarMapState GenerateStarMapStateFromMap(List<MapChunk> myMap) {
	    
	    var starMapState = new StarMapState();
	    for (int i = 0; i < myMap.Count; i++) {
		    var chunk = new StarMapChunk();

		    for (int j = 0; j < myMap[i].possibleStarCounts[0]; j++) {
			    chunk.myStars.Add(CreateStarState(i));
		    }

		    starMapState.chunks.Add(chunk);
	    }

	    return starMapState;
    }

    void ConnectAllStars(StarMapState starMapState) {
	    for (int i = 0; i < starMapState.chunks.Count - 1; i++) {
		    ConnectStarGroups(starMapState.chunks[i], starMapState.chunks[i + 1]);
	    }
    }

    private void MakeBoss(StarMapState starMapState) {
	    MakeBossStar(starMapState.chunks[bossDistance - 1].myStars[0], starMapState.chunks[bossDistance-2]);
    }

    void MakeBossStar(StarState starState, StarMapChunk leadingChunk) {
	    starState.isBoss = true;
	    starState.rewardCart = 1;
	    //starState.level = bossLevels[Random.Range(0, bossLevels.Length)].myData;
	    var actData = GetCurrentAct();
	    
	    starState.city = actData.bossCities[Random.Range(0, actData.bossCities.Length)].cityData;

	    var bossLevelPick = actData.bossLevels[Random.Range(0, actData.bossLevels.Length)].GenerateLevel();

	    for (int i = 0; i < leadingChunk.myStars.Count; i++) {
		    var levels = leadingChunk.myStars[i].outgoingConnectionLevels;
		    for (int j = 0; j < levels.Count; j++) {
			    levels[j] = bossLevelPick;
		    }
	    }
    }

    readonly string[] prefixes = new []{"Slam", "Slo","Ose","Yellow","Flu", "Nice", "Bad", "Great", "Bear"};
	readonly string[] suffixes = new []{"ford", "hill", "ville", "wood", "burg", "bridge", "ton"};
	private HashSet<string> givenStarNames = new HashSet<string>();

    private StarState CreateStarState(int starChunk) {
	    var actData = GetCurrentAct();
	    
	    var count = 0;
	    var potentialStarName = $"{prefixes[Random.Range(0, prefixes.Length)]}{suffixes[Random.Range(0, suffixes.Length)]}";
	    while (givenStarNames.Contains(potentialStarName) && count < 100) {
		    count += 1;
		    potentialStarName = $"{prefixes[Random.Range(0, prefixes.Length)]}{suffixes[Random.Range(0, suffixes.Length)]}";
	    }

	    if (count >= 100) {
		    potentialStarName = "Kekville";
	    }

	    givenStarNames.Add(potentialStarName);



	    CityData city;
	    if (starChunk <= 1) {
		    city = GetCityData(actData.firstCities).cityData;
	    } else {
		    city = GetCityData(actData.remainingCities).cityData;
	    }
	    
	    var info = new StarState($"{potentialStarName} {city.nameSuffix}");
	    
	    info.city = city;

	    info.starChunk = starChunk;

	    info.currentAct = currentAct;
	    info.biome = currentAct - 1;

	    if (starChunk >= bossDistance) {
		    info.biome += 1;
		    info.biome = Mathf.Clamp(info.biome, 0, 2);
	    }
	    
	    return info;
    }


    public CityDataScriptable GetCityData(CityDataScriptable[] pool) {
	    var weights = new List<NumberWithWeights>();

	    for (int i = 0; i < pool.Length; i++) {
		    weights.Add(new NumberWithWeights(){number=i, weight = pool[i].cityData.cityRarity});
	    }

	    var randIndex = NumberWithWeights.WeightedRandomRoll(weights.ToArray());

	    return pool[randIndex];
    }

    /*private List<int> MakeShops(StarMapState starMapState) {
	    var miniPortalLocations = new List<int>();

	    for (int i = miniPortalDistance; i < timePortalDistance; i += miniPortalDistance) {
		    miniPortalLocations.Add(i + Random.Range(-miniPortalVariance, +miniPortalVariance));
	    }

	    for (int i = 0; i < miniPortalLocations.Count; i++) {
		    var shopCandidates = starMapState.chunks[miniPortalLocations[i]].myStars;
		    MakeShopStar(shopCandidates[Random.Range(0, shopCandidates.Count)]);
	    }

	    return miniPortalLocations;
    }*/
    

    /*void MakeShopStar(StarState starState) {
	    starState.isShop = true;
    }*/

    /*private void SpawnStarsAndConnections(StarMapState starMapState) {
	    parent.DeleteAllChildren();
	    connectorsParent.DeleteAllChildren();

	    var activeStarGroups = new List<GameObject>();

	    for (int i = 0; i < starMapState.chunks.Count; i++) {
		    var chunk = starMapState.chunks[i];
		    activeStarGroups.Add(Instantiate(starGroups[chunk.myStars.Count - 1], parent));

		    var stars = activeStarGroups[i].GetComponentsInChildren<MiniGUI_Star>();
		    for (int j = 0; j < stars.Length; j++) {
			    var myStar = chunk.myStars[j];
			    stars[j].Initialize(myStar);
		    }
	    }

	    for (int i = 0; i < activeStarGroups.Count-1; i++) {
		    var stars = activeStarGroups[i].GetComponentsInChildren<MiniGUI_Star>();
		    for (int j = 0; j < stars.Length; j++) {
			    var curStar = stars[j];

			    var nextStars = activeStarGroups[i + 1].GetComponentsInChildren<MiniGUI_Star>();

			    for (int k = 0; k < curStar.myInfo.outgoingConnections.Count; k++) {
				    var a = curStar;
				    var b = GetStarFromStarName(nextStars, curStar.myInfo.outgoingConnections[k]);
				    
				    var connection = Instantiate(connectingLine, connectorsParent).GetComponent<MiniGUI_StarConnection>();
				    connection.Initialize(a,b);

				    if (a.myInfo.isPlayerHere) {
					    connection.SetHighlightState(true);
					    b.currentlyPlayerTravelable = true;
				    }else if (a.myInfo.previouslyVisited && b.myInfo.previouslyVisited) {
					    connection.SetPreviouslyVisited();
				    }

				    connection.SetEncounter(a.myInfo.outgoingConnectionLevels[k].isEncounter);
			    }
		    }
	    }
    }*/

    MiniGUI_Star GetStarFromStarName(MiniGUI_Star[] stars, string starName) {
	    for (int i = 0; i < stars.Length; i++) {
		    if (stars[i].myInfo.starName == starName) {
			    return stars[i];
		    }
	    }

	    return null;
    }

    private void GenerateMapWithWaveCollapse(List<MapChunk> myMap) {
	    var totalStarCount = 0;
	    var curGeneration = 0;
	    while ((totalStarCount < minStarCount || totalStarCount > maxStarCount) && curGeneration < 20) {
		    myMap.Clear();
		    for (int i = 0; i < bossDistance; i++) {
			    myMap.Add(new MapChunk());
		    }

		    myMap[bossDistance - 1].possibleStarCounts = new List<int>() { 1 };
		    myMap[0].possibleStarCounts = new List<int>() { 1 };

		    WaveFunctionCollapse(myMap, bossDistance - 1, 1);
		    WaveFunctionCollapse(myMap, 0, 1);

		    var randomPoint = Random.Range(1, bossDistance);
		    var randomStarCount = Random.Range(1, 4);
		    while (!WaveFunctionCollapse(myMap, randomPoint, randomStarCount)) {
			    randomPoint = Random.Range(1, bossDistance);
			    randomStarCount = Random.Range(1, 4);
		    }

		    totalStarCount = 0;
		    for (int i = 0; i < myMap.Count; i++) {
			    totalStarCount += myMap[i].possibleStarCounts[0];
		    }

		    curGeneration += 1;
	    }

	    Debug.Log($"Generated Star Map with {totalStarCount} stars in {curGeneration} generations");
    }


    //public MiniGUI_StarInfoPanel targetStarInfoScreen;
    public MiniGUI_StarInfoPanel targetStarInfoScreenBudget;
    //public MiniGUI_StarInfoPanel selectedStarInfoScreen;

    //public StarState playerStar => StateMaster.s.currentState.starMapState.GetPlayerStar();
    public StarState playerStar => DataSaver.s.GetCurrentSave().currentRun.map.GetPlayerStar();
	private StarState targetStar;
    public void ShowStarInfo(StarState star, BoolReturnCallback callback = null) {
	    targetStar = star;
	    
	    var _playerStar = DataSaver.s.GetCurrentSave().currentRun.map.GetPlayerStar();
	    ConstructedLevel level = null;
	    for (int i = 0; i < _playerStar.outgoingConnections.Count; i++) {
		    if (_playerStar.outgoingConnections[i] == targetStar.starName) {
			    level = _playerStar.outgoingConnectionLevels[i];
		    }
	    }
	    
	    //targetStarInfoScreen.Initialize(targetStar, level);
	    targetStarInfoScreenBudget.Initialize(targetStar, level, callback); //WORLDMAP
	    
	    //targetStarInfoScreen.SetSelectable(!SceneLoader.s.isLevelStarted() && level != null);
	    targetStarInfoScreenBudget.SetSelectable(!PlayStateMaster.s.isCombatStarted() && level!=null); //WORLDMAP
    }

    public void SelectStar() {
	    targetStarInfoScreenBudget.Hide(); //WORLDMAP
	    WorldMapCreator.s.ReturnToRegularMap(); //WORLDMAP
	    
	    //targetStarInfoScreen.Hide();
	    
	    mapUI.HideMenu();
	    
	    var _playerStar = DataSaver.s.GetCurrentSave().currentRun.map.GetPlayerStar();
	    ConstructedLevel level = null;
	    for (int i = 0; i < _playerStar.outgoingConnections.Count; i++) {
		    if (_playerStar.outgoingConnections[i] == targetStar.starName) {
			    level = _playerStar.outgoingConnectionLevels[i];
		    }
	    }
	    
	    //selectedStarInfoScreen.Initialize(_playerStar, targetStar, level);
	    
	    ShopStateController.s.SelectLevel(targetStar);
	    AskForCargo(false);
    }

    public TMP_Text travelButtonText;
    public bool isAskingCargo = false;
    public void StartTravelingToStar() {
	    if (isAskingCargo || Train.s.GetCargoCount() > 0 ) {
		    travelToStarWaiting = true;
		    ShopStateController.s.StartLevel();
		    AskForCargo(false);
	    } else {
		    AskForCargo(true);
	    }
    }

    public void AskForCargo(bool state) {
	    isAskingCargo = state;
	    if (isAskingCargo) {
		    travelButtonText.text = "Are you sure you want to leave without cargo?";
	    } else {
		    travelButtonText.text = "Travel";
	    }
	    //Invoke(nameof(DeActivateAskForCargo), 1f);
    }

    /*void DeActivateAskForCargo() {
	    AskForCargo(false);
    }*/


    public bool travelToStarWaiting = false;
    public void FinishTravelingToStar() {
	    if (travelToStarWaiting) {
		    RemovePlayerFromStar();

		    PutPlayerInStar(targetStar);

		    //ApplyStarMapFromSave();
	    }
	    
	    Cleanup();
    }

    public void TravelToStar(StarState starState) {
	    if (starState != null) {
		    RemovePlayerFromStar();
		    PutPlayerInStar(starState);
		    Cleanup();
	    }
    }

    public void DebugTravelToSelectStar() {
	    travelToStarWaiting = true;

	    FinishTravelingToStar();
	    WorldMapCreator.s.ToggleWorldMap();
	    WorldMapCreator.s.ToggleWorldMap();
    }

    public void RemovePlayerFromStar() {
	    playerStar.isPlayerHere = false;
    }
    
    public void PutPlayerInStar(StarState starState) {
	    starState.isPlayerHere = true;
	    starState.previouslyVisited = true;
    }

    public void DeselectStar() {
	    //targetStarInfoScreen.Hide();
	    //selectedStarInfoScreen.Hide();
	    targetStarInfoScreenBudget.Hide();
    }


    bool WaveFunctionCollapse(List<MapChunk> map, int index, int starCount) {
	    if (map[index].possibleStarCounts.Contains(starCount)) {
		    map[index].possibleStarCounts = new List<int>() { starCount};
	    } else {
		    return false;
	    }

	    
	    for (int i = index+1; i < map.Count; i++) {
		    var a = map[i-1];
		    var b = map[i];
		    
		    CollapseTwoChunks(a, b);
	    }


	    for (int i = index - 1; i >= 0; i--) {
		    var a = map[i+1];
		    var b = map[i];
		    
		    CollapseTwoChunks(a, b);
	    }


	    var fullyCollapsed = true;
	    for (int i = 0; i < map.Count; i++) {
		    if (map[i].possibleStarCounts.Count > 1) {
			    fullyCollapsed = false;
			    break;
		    }
	    }

	    return fullyCollapsed;
    }

    private static void CollapseTwoChunks(MapChunk a, MapChunk b) {
	    var legalSpots = new List<int>();
	    for (int j = 0; j < a.possibleStarCounts.Count; j++) {
		    var sourceNum = a.possibleStarCounts[j];
		    for (int k = 0; k < b.possibleStarCounts.Count; k++) {
			    var compNum = b.possibleStarCounts[k];
			    if (MapChunk.isLegalNeighbor(sourceNum, compNum)) {
				    if(!legalSpots.Contains(compNum))
						legalSpots.Add(compNum);
			    }
		    }
	    }
	    b.possibleStarCounts = legalSpots;
    }

    void ConnectStarGroups(StarMapChunk a, StarMapChunk b) {
	    var aStars = a.myStars;
	    var bStars = b.myStars;

	    var randomLayout = Random.Range(0, 100);
	    switch (aStars.Count) {
		    case 1:
			    switch (bStars.Count) {
				    case 1: // a = 1, b = 1
					    ConnectStars(aStars[0], bStars[0]);
					    break;
				    case 2: // a = 1, b = 2
					    ConnectStars(aStars[0], bStars[0]);
					    ConnectStars(aStars[0], bStars[1]);
					    break;
			    }
			    break;
		    case 2:
			    switch (bStars.Count) {
				    case 1: // a = 2, b = 1
					    ConnectStars(aStars[0], bStars[0]);
					    ConnectStars(aStars[1], bStars[0]);
					    break;
				    case 2: // a = 2, b = 2
					    switch (randomLayout % 3) {
						    case 0:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[1], bStars[1]);
							    break;
						    case 1:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[0], bStars[1]);
							    ConnectStars(aStars[1], bStars[1]);
							    break;
						    case 2:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[1], bStars[0]);
							    ConnectStars(aStars[1], bStars[1]);
							    break;
					    }
					    break;
				    case 3: // a = 2, b = 3
					    switch (randomLayout % 3) {
						    case 0:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[0], bStars[1]);
							    ConnectStars(aStars[1], bStars[1]);
							    ConnectStars(aStars[1], bStars[2]);
							    break;
						    case 1:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[1], bStars[1]);
							    ConnectStars(aStars[1], bStars[2]);
							    break;
						    case 2:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[0], bStars[1]);
							    ConnectStars(aStars[1], bStars[2]);
							    break;
					    }
					    break;
			    }
			    break;
		    case 3:
			    switch (bStars.Count) {
				    case 2: // a = 3, b = 2
					    switch (randomLayout % 3) {
						    case 0:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[1], bStars[0]);
							    ConnectStars(aStars[1], bStars[1]);
							    ConnectStars(aStars[2], bStars[1]);
							    break;
						    case 1:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[1], bStars[1]);
							    ConnectStars(aStars[2], bStars[1]);
							    break;
						    case 2:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[1], bStars[0]);
							    ConnectStars(aStars[2], bStars[1]);
							    break;
					    }
					    break;
				    case 3: // a = 3, b = 3
					    switch (randomLayout % 4) {
								// all possibilities
							    /*ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[0], bStars[1]);
							    ConnectStars(aStars[1], bStars[0]);
							    ConnectStars(aStars[1], bStars[1]);
							    ConnectStars(aStars[1], bStars[2]);
							    ConnectStars(aStars[2], bStars[1]);
							    ConnectStars(aStars[2], bStars[2]);*/
						    case 0:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[1], bStars[1]);
							    ConnectStars(aStars[2], bStars[2]);
							    break;
						    /*case 1:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[1], bStars[0]);
							    ConnectStars(aStars[1], bStars[1]);
							    ConnectStars(aStars[1], bStars[2]);
							    ConnectStars(aStars[2], bStars[2]);
							    break;*/
						    case 1:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[0], bStars[1]);
							    ConnectStars(aStars[1], bStars[1]);
							    ConnectStars(aStars[1], bStars[2]);
							    ConnectStars(aStars[2], bStars[2]);
							    break;
						    case 2:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[1], bStars[0]);
							    ConnectStars(aStars[1], bStars[1]);
							    ConnectStars(aStars[2], bStars[1]);
							    ConnectStars(aStars[2], bStars[2]);
							    break;
						    case 3:
							    ConnectStars(aStars[0], bStars[0]);
							    ConnectStars(aStars[0], bStars[1]);
							    ConnectStars(aStars[1], bStars[1]);
							    ConnectStars(aStars[2], bStars[1]);
							    ConnectStars(aStars[2], bStars[2]);
							    break;
					    }
					    break;
			    }
			    break;
	    }
    }

    private void ConnectStars(StarState a, StarState b) {
	    a.outgoingConnections.Add(b.starName);
	    
	    var actData = GetCurrentAct();
	    
	    /*var rollEncounter = Random.value < encounterChance;
	    LevelData level;
	    if (rollEncounter && a.starChunk >= 2) {
		    var encounterName = actData.encounters[Random.Range(0, actData.encounters.Length)].gameObject.name;
		    a.outgoingConnectionLevels.Add("e_" + encounterName);
	    }else{
		    if (a.starChunk < 2) {
			    level = actData.firstLevels[Random.Range(0, actData.firstLevels.Length)].GetData().Copy();
		    } else {
			    level = actData.remainingLevels[Random.Range(0, actData.remainingLevels.Length)].GetData().Copy();
		    }
		    
		    a.outgoingConnectionLevels.Add(level.levelName);
	    }*/

	    ConstructedLevel level;
	    if (a.starChunk < firstLevelDistance) {
		    level = actData.firstLevels[Random.Range(0, actData.firstLevels.Length)].GenerateLevel();
	    } else {
		    level = actData.remainingLevels[Random.Range(0, actData.remainingLevels.Length)].GenerateLevel();
	    }
	    
	    a.outgoingConnectionLevels.Add(level);



	    /*var cargoType = cargoTiers[Random.Range(0, cargoTiers.Length)];
	    
	    var cargoCost = cargoType.cost * (1f+Random.Range(-cargoType.costVariance, +cargoType.costVariance));
	    var cargoReward = cargoType.reward * (1f+Random.Range(-cargoType.rewardVariance, +cargoType.rewardVariance));

	    if (b.starChunk == timePortalDistance - 1) {
		    cargoCost *= 2f;
		    cargoReward *= 2f;
	    }
	    var cargoData = new CargoDeliverMissionData() { cost = (int)cargoCost, reward = (int)cargoReward };
	    a.outgoingConnectionCargoData.Add(cargoData);*/
    }

    class MapChunk {
	    public List<int> possibleStarCounts = new List<int>(){3,2,1};

	    public static bool isLegalNeighbor(int a, int b) {
		    return Mathf.Abs(a - b) <= 1;
	    }
    }

    public void Cleanup() {
	    DeselectStar();
    }
}



[Serializable]
public class StarMapState {
	public List<StarMapChunk> chunks = new List<StarMapChunk>();
	/*public StarState GetStarStateFromStarName(string starName) {
		for (int i = 0; i < chunks.Count; i++) {
			for (int j = 0; j < chunks[i].myStars.Count; j++) {
				if (chunks[i].myStars[j].starName == starName)
					return chunks[i].myStars[j];
			}
		}
		return null;
	}*/

	public StarState GetPlayerStar() {
		for (int i = 0; i < chunks.Count; i++) {
			for (int j = 0; j < chunks[i].myStars.Count; j++) {
				if (chunks[i].myStars[j].isPlayerHere)
					return chunks[i].myStars[j];
			}
		}
		return null;
	}
	
	public StarState GetStarWithName(string starName) {
		for (int i = 0; i < chunks.Count; i++) {
			for (int j = 0; j < chunks[i].myStars.Count; j++) {
				if (chunks[i].myStars[j].starName == starName)
					return chunks[i].myStars[j];
			}
		}
		return null;
	}
}


[Serializable]
public class StarMapChunk {
	public List<StarState> myStars = new List<StarState>();
}

[Serializable]
public class StarState {
	[Tooltip("Must be Unique")]
	public string starName;
	public bool isShop;
	public bool isBoss;
	public bool isPlayerHere;
	public bool previouslyVisited;
	//public int starVariation = -1;
	public int starChunk = -1;
	public CityData city;
	public int rewardCart = 0;
	public int rewardMoney = 0;
	
	public int currentAct = 0;
	public int biome = 0;

	public List<string> outgoingConnections = new List<string>();
	public List<ConstructedLevel> outgoingConnectionLevels = new List<ConstructedLevel>();
	//public List<CargoDeliverMissionData> outgoingConnectionCargoData = new List<CargoDeliverMissionData>();
	//public List<CargoDeliverMissionData> cargoData = new List<CargoDeliverMissionData>();

	public StarState(string _starName) {
		starName = _starName;
	}
}

[Serializable]
public class CargoDeliverMissionData {
	public int cost = 50;
	public int reward = 150;
}

/*[Serializable]
public class CargoPrices {
	public int cost = 50;
	public float costVariance = 0.2f;
	public int reward = 150;
	public float rewardVariance = 0.2f;
}*/