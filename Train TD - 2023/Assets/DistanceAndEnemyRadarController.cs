using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DistanceAndEnemyRadarController : MonoBehaviour {
    public static DistanceAndEnemyRadarController s;

    private void Awake() {
        s = this;
    }

    public List<IShowOnDistanceRadar> myUnits = new List<IShowOnDistanceRadar>();

    public Transform unitsParent;
    public GameObject unitsPrefab;
    public GameObject trainPrefab;

    public List<GameObject> unitDisplays = new List<GameObject>();

    public RectTransform unitsArea;

    [NonSerialized]
    public float UISizeMultiplier = 4;

    public void RegisterUnit(IShowOnDistanceRadar unit) {
        myUnits.Add(unit);
        if (unit.IsTrain()) {
            unitDisplays.Add(Instantiate(trainPrefab, unitsParent));
            unitDisplays[unitDisplays.Count - 1].transform.GetChild(0).GetComponent<Image>().sprite = unit.GetIcon();
        } else {
            unitDisplays.Add(Instantiate(unitsPrefab, unitsParent));
            unitDisplays[unitDisplays.Count - 1].GetComponent<MiniGUI_RadarUnit>().SetUp(unit.GetIcon(), unit.isLeftUnit(), -1);
            
            var totalDistance = SpeedController.s.missionDistance;
            var width = unitsArea.rect.width;
            var percentage = unit.GetDistance() / totalDistance;
            var distance = percentage * width;
            unitDisplays[unitDisplays.Count - 1].GetComponent<RectTransform>().anchoredPosition = new Vector2(distance, baseHeight);
        }

        Update();
    }

    public void RemoveUnit(IShowOnDistanceRadar unit) {
        var index = myUnits.IndexOf(unit);

        if (index > 0) {
            myUnits.RemoveAt(index);
            Destroy(unitDisplays[index]);
            unitDisplays.RemoveAt(index);
        }
    }

    public float baseHeight = 25f;
    public float increaseHeight = 25f;
    // Update is called once per frame
    void Update() {
        var lastDistance = float.NegativeInfinity;
        var curHeight = 0f;
        //myUnits.Sort((x, y) => x.GetDistance().CompareTo(y.GetDistance()));
        for (int i = 0; i < myUnits.Count; i++) {

            /*var percentage = myUnits[i].GetDistance() / totalDistance;
            var distance = percentage * width;*/
            var distance = myUnits[i].GetDistance();

            if (Mathf.Abs(lastDistance - distance) < 25) {
                curHeight += 1;
            } else {
                curHeight = 0;
            }

            distance -= PathAndTerrainGenerator.s.currentPathTreeOffset;
            var pathLength = PathAndTerrainGenerator.s.currentPathTree.myPath.length;
            
            if (PathAndTerrainGenerator.s.currentPathTree.startPath) {
                distance -= PathGenerator.stationStraightDistance / 2f;
                pathLength -= PathGenerator.stationStraightDistance / 2f;
            }

            if (PathAndTerrainGenerator.s.currentPathTree.endPath) {
                pathLength -= PathGenerator.stationStraightDistance / 2f + 9f;
            }

            var percentage = distance / pathLength;
            var howFarToGo = percentage * ((RectTransform)unitsParent).rect.width;
            
            var targetPos = Vector2.Lerp(
                unitDisplays[i].GetComponent<RectTransform>().anchoredPosition,
                new Vector2(howFarToGo/*distance*UISizeMultiplier*/, baseHeight + (curHeight * increaseHeight)),
                10 * Time.deltaTime
            );


            unitDisplays[i].GetComponent<RectTransform>().anchoredPosition = targetPos;
                

            lastDistance = distance;
        }
    }

    public void ClearRadar() {
        for (int i = 0; i < unitDisplays.Count; i++) {
            Destroy(unitDisplays[i]);
        }
        
        myUnits.Clear();
        unitDisplays.Clear();
        
        SpeedController.s.RegisterRadar();
    }
}

public interface IShowOnDistanceRadar {
    public bool IsTrain();
    public float GetDistance();
    public Sprite GetIcon();
    public bool isLeftUnit();
}
