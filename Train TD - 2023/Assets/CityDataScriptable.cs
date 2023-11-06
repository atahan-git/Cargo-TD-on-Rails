using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CityDataScriptable : ScriptableObject {
	public Sprite sprite;
	public GameObject worldMapCastle;
	public CityData cityData;
}


[Serializable]
public class CityData {
	public float cityRarity = 1f;

	public string uniqueName;
	public string nameSuffix;

	public List<BuildingType> myBuildings;
	public enum BuildingType {
		fleaMarket=0, fleaMarket_artifactOnly=1, smithy=2, recycler=3, scrapper=4
	}
}
