using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGUI_CityIcons : MonoBehaviour {

    public GameObject flea;

    public GameObject artifact;

    public GameObject recycler;

    public GameObject scrapper;

    public GameObject smithery;


    public void SetState(CityData data) {
        var buildings = data.myBuildings;
        flea.SetActive(buildings.Contains(CityData.BuildingType.fleaMarket));
        artifact.SetActive(buildings.Contains(CityData.BuildingType.fleaMarket_artifactOnly));
        recycler.SetActive(buildings.Contains(CityData.BuildingType.recycler));
        scrapper.SetActive( buildings.Contains(CityData.BuildingType.scrapper));
        smithery.SetActive(buildings.Contains(CityData.BuildingType.smithy));
    }
}
