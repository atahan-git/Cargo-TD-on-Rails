using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_TrackPath : MonoBehaviour {
    public int trackId;

    public float targetWidth;
    
    public GameObject unitsPrefab;
    
    public List<GameObject> unitDisplays = new List<GameObject>();
    private float baseHeight => DistanceAndEnemyRadarController.s.baseHeight;
    
    private ManualLayoutElement _layoutElement;
    private ManualHorizontalLayoutGroup _layoutGroup;
    
    
    public Color disabledColor = Color.white;

    private void Start() {
        _layoutElement = GetComponent<ManualLayoutElement>();
        _layoutGroup = GetComponentInParent<ManualHorizontalLayoutGroup>(true);
    }

    public void SetUpTrack(float _length) {
        targetWidth = _length;
        
        widthDirty = true;
    }
    
    

    void SpawnUnitsOnSegment(/*LevelSegment segment,*/ RectTransform parent) {
        //if (!segment.isEncounter) {
            /*for (int i = 0; i < segment.enemiesOnPath.Length; i++) {
                var unitIcon = DataHolder.s.GetEnemy(segment.enemiesOnPath[i].enemyIdentifier.enemyUniqueName).GetComponent<EnemySwarmMaker>().enemyIcon;
                if (segment.rewardPowerUpAtTheEnd && i == segment.enemiesOnPath.Length - 1) {
                    unitIcon = DataHolder.s.GetPowerUp(segment.powerUpRewardUniqueName).icon;
                }

                var percentage = (float)segment.enemiesOnPath[i].distanceOnPath / segment.segmentLength;

                var unit = Instantiate(unitsPrefab, parent);
                unitDisplays.Add(unit);
                unit.GetComponent<MiniGUI_RadarUnit>().SetUp(unitIcon, segment.enemiesOnPath[i].isLeft, percentage);
            }*/
            
            var percentage = 20/parent.rect.width;
            //var distance = percentage * parent.rect.width;
            
            var unit = Instantiate(unitsPrefab, parent);
            unitDisplays.Add(unit);

            /*var icon = LevelReferences.s.smallEnemyIcon;
            if (segment.eliteEnemy)
                icon = LevelReferences.s.eliteEnemyIcon;
            if (segment.isEncounter)
                icon = LevelReferences.s.encounterIcon;
            
            
            // better icons
            if (!segment.isEncounter) {
                icon = DataHolder.s.GetEnemy(segment.enemiesOnPath[0].enemyIdentifier.enemyUniqueName).GetComponent<EnemySwarmMaker>().enemyIcon;

                for (int i = 0; i < segment.enemiesOnPath.Length; i++) {
                    var enemy = segment.enemiesOnPath[i];
                    
                    if (enemy.useAsIcon) {
                        icon = DataHolder.s.GetEnemy(enemy.enemyIdentifier.enemyUniqueName).GetComponent<EnemySwarmMaker>().enemyIcon;
                        break;
                    } else {
                        if (enemy.hasReward) {
                            icon = DataHolder.s.GetEnemy(enemy.enemyIdentifier.enemyUniqueName).GetComponent<EnemySwarmMaker>().enemyIcon;
                        } 
                    }
                }
            }
            
            
            unit.GetComponent<MiniGUI_RadarUnit>().SetUp(icon, percentage, segment.eliteEnemy, segment.isEncounter);*/
            
        /*} else {
            var percentage = 0.5f;
            //var distance = percentage * parent.rect.width;
            
            var unit = Instantiate(unitsPrefab, parent);
            unitDisplays.Add(unit);
            unit.GetComponent<MiniGUI_RadarUnit>().SetUp(LevelReferences.s.encounterIcon, percentage);
        }*/
    }

    public bool widthDirty = false;
    private float uiSizeMultiplier => DistanceAndEnemyRadarController.s.UISizeMultiplier;
    public float pixelMultiplier = 1f;

    void UpdateLengths() {
        
        var lastWidth = _layoutElement.preferredWidth;
        _layoutElement.preferredWidth = Mathf.Lerp(_layoutElement.preferredWidth, targetWidth, 10* Time.deltaTime);

        if (Mathf.Abs(lastWidth - _layoutElement.preferredWidth) > 0.1f) {
            widthDirty = true;
        }

        if (widthDirty || _layoutGroup.isLocationsDirty) {
            //set segment widths:
            var currentWidth = GetComponent<RectTransform>().rect.width;
            
            /*topTrack.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _segmentA.segmentLength*selectionScaleMultiplier);
            bottomTrack.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _segmentB.segmentLength*selectionScaleMultiplier);*/

            GetComponentInChildren<Image>().pixelsPerUnitMultiplier = 6f * (targetWidth / currentWidth) * uiSizeMultiplier * pixelMultiplier;
        }
    }

    private void Update() {
        UpdateLengths();
    }

    void ClearUnitDisplays() {
        for (int i = unitDisplays.Count-1; i >= 0; i--) {
            Destroy(unitDisplays[i].gameObject);
        }
        
        unitDisplays.Clear();
    }

    public void SetActiveState(bool state) {
        if (state) {
            GetComponentInChildren<Image>().color = Color.white;
        } else {
            GetComponentInChildren<Image>().color = disabledColor;
        }
    }
    
    /*void SetAndStretchToParentSize(GameObject target) {
        var _mRect = target.GetComponent<RectTransform>();
        _mRect.anchorMin = Vector2.zero;
        _mRect.anchorMax = Vector2.one;
        _mRect.pivot = new Vector2(0, 0.5f);
        _mRect.sizeDelta = Vector2.zero;
        _mRect.anchoredPosition = Vector2.zero;
    }*/
}
