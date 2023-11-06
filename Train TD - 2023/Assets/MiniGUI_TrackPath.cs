using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_TrackPath : MonoBehaviour {
    public int trackId;

    public bool currentState;
    public bool isShowingBoth = false;
    
    public GameObject unitsPrefab;
    
    public List<GameObject> unitDisplays = new List<GameObject>();

    private float baseHeight => DistanceAndEnemyRadarController.s.baseHeight;

    public Color regularShowColor = Color.white;
    private LevelSegment _segmentA;
    private LevelSegment _segmentB;

    private ManualLayoutElement _layoutElement;
    private ManualHorizontalLayoutGroup _layoutGroup;

    private void Start() {
        _layoutElement = GetComponent<ManualLayoutElement>();
        _layoutGroup = GetComponentInParent<ManualHorizontalLayoutGroup>(true);
    }

    public void SetUpTrack(bool selectedTrackState, LevelSegment mySegmentDataA, LevelSegment mySegmentDataB) {
        currentState = selectedTrackState;
        _segmentA = mySegmentDataA;
        _segmentB = mySegmentDataB;
        
        //SetTrackState(currentState, true);

        GetComponent<ManualLayoutElement>().preferredWidth = currentState ? _segmentA.segmentLength : _segmentB.segmentLength;
        widthDirty = true;
        
        showBothObject.transform.position = hidePosition.position;
    }
    
    

    void SpawnUnitsOnSegment(LevelSegment segment, RectTransform parent) {
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

            var icon = LevelReferences.s.smallEnemyIcon;
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
            
            
            unit.GetComponent<MiniGUI_RadarUnit>().SetUp(icon, percentage, segment.eliteEnemy, segment.isEncounter);
            
        /*} else {
            var percentage = 0.5f;
            //var distance = percentage * parent.rect.width;
            
            var unit = Instantiate(unitsPrefab, parent);
            unitDisplays.Add(unit);
            unit.GetComponent<MiniGUI_RadarUnit>().SetUp(LevelReferences.s.encounterIcon, percentage);
        }*/
    }

    public void TrackClicked() {
        if (!isLocked) {
            PathSelectorController.s.ShowTrackInfo(trackId);
        }
    }

    public void ToggleTrackState(bool _isShowingBoth) { 
        isShowingBoth = _isShowingBoth;
        showBothObject.SetActive(true);
        showSingleObject.SetActive(false);
        if(singleLever != null)
            singleLever.SetVisibility(false);
        isMoving = true;
        curReverseSpeed = 0;
    }

    public void SetTrackState(bool state, bool forced = false) {  // True is A
        if (isShowingBoth) {
            isStateDirty = true;
            dirtyState = state;
            return;
        }

        if (state != currentState || forced) {
            currentState = state;

            ClearUnitDisplays();
            SpawnUnitsOnSegment(_segmentA, topTrack);
            SpawnUnitsOnSegment(_segmentB, bottomTrack);


            Destroy(showSingleObject);
            showSingleObject = Instantiate(currentState ? topTrack.gameObject : bottomTrack.gameObject, showSingleParent);
            showSingleObject.GetComponent<Image>().color = regularShowColor;
            SetAndStretchToParentSize(showSingleObject);
            if (isShowingBoth) {
                showSingleObject.SetActive(false);
            }
        }
    }

    public bool widthDirty = false;
    private float uiSizeMultiplier => DistanceAndEnemyRadarController.s.UISizeMultiplier;

    void UpdateLengths() {
        var targetWidth = currentState ? _segmentA.segmentLength : _segmentB.segmentLength;

        var lastWidth = _layoutElement.preferredWidth;
        _layoutElement.preferredWidth = Mathf.Lerp(_layoutElement.preferredWidth, targetWidth, 10* Time.deltaTime);

        if (Mathf.Abs(lastWidth - _layoutElement.preferredWidth) > 0.1f) {
            widthDirty = true;
        }

        var selectionScaleMultiplier = 1f;
        
        if (widthDirty || _layoutGroup.isLocationsDirty) {
            //set segment widths:
            var currentWidth = GetComponent<RectTransform>().rect.width;
            /*var currentWidth = GetComponent<RectTransform>().rect.width;
            
            if (currentState) {
                var ratioA = 1f;
                var ratioB = (float)_segmentB.segmentLength / (float)_segmentA.segmentLength;

                if (ratioB > 1f) {
                    ratioA = 1f / ratioB;
                    ratioB = 1f;
                }
                
                topTrack.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth * ratioA);
                bottomTrack.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth * ratioB) ;
                //print($"{currentState} - {currentWidth} - {currentWidth * ((float)_segmentB.segmentLength / _segmentA.segmentLength)}");
            } else {
                var ratioA = (float)_segmentA.segmentLength / (float)_segmentB.segmentLength;
                var ratioB = 1f;

                if (ratioA > 1f) {
                    ratioB = 1f / ratioA;
                    ratioA = 1f;
                }

                topTrack.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth * ratioA);
                bottomTrack.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth * ratioB);

                //print($"{currentState} - {currentWidth * ((float)_segmentA.segmentLength / _segmentB.segmentLength)} - {currentWidth}");
            }*/
            
            topTrack.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _segmentA.segmentLength*selectionScaleMultiplier);
            bottomTrack.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _segmentB.segmentLength*selectionScaleMultiplier);

            showSingleObject.GetComponent<Image>().pixelsPerUnitMultiplier = 6f * (targetWidth / currentWidth) * uiSizeMultiplier;
            topTrack.GetComponent<Image>().pixelsPerUnitMultiplier = 
                6f * (_segmentA.segmentLength / topTrack.GetComponent<RectTransform>().rect.width) * (selectionScaleMultiplier*2f);
            bottomTrack.GetComponent<Image>().pixelsPerUnitMultiplier = 
                6f * (_segmentB.segmentLength / bottomTrack.GetComponent<RectTransform>().rect.width) * (selectionScaleMultiplier*2f);
        }
    }

    public Transform showPosition;
    public Transform hidePosition;
    public GameObject showBothObject;
    
    public RectTransform topTrack;
    public RectTransform bottomTrack;
    
    public Transform showSingleParent;
    public GameObject showSingleObject;

    public MiniGUI_TrackLever singleLever;
    public MiniGUI_TrackLever doubleLever;

    private float reverseLerpAcceleration = 200;
    private float lerpSpeed = 15;
    private float curReverseSpeed = 0;
    
    
    public bool isMoving = false;
    public bool isStateDirty = false;
    private bool dirtyState;
    private void Update() {
        if (isMoving) {
            if (isShowingBoth) {
                showBothObject.transform.position = Vector3.Lerp(showBothObject.transform.position, showPosition.position, lerpSpeed * Time.deltaTime);
                if (Mathf.Abs(showBothObject.transform.position.y - showPosition.position.y) < 0.1f) {
                    isMoving = false;
                    showBothObject.transform.position = showPosition.position;
                }
            } else {
                showBothObject.transform.position = Vector3.Lerp(showBothObject.transform.position, hidePosition.position, curReverseSpeed * Time.deltaTime);
                curReverseSpeed += reverseLerpAcceleration * Time.deltaTime;

                if (Mathf.Abs(showBothObject.transform.position.y - hidePosition.position.y) < 0.3f) {
                    isMoving = false;
                    showBothObject.SetActive(false);
                    showSingleObject.SetActive(true);
                    if(singleLever != null)
                        singleLever.SetVisibility(true);
                    showBothObject.transform.position = hidePosition.position;
                }
            }
        }

        UpdateLengths();

        if (isStateDirty && !isShowingBoth) {
            isStateDirty = false;
            SetTrackState(dirtyState);
        }
    }


    private bool isLocked = false;
    public void LockTrackState() {
        isLocked = true;
        ToggleTrackState(false);
        ClearUnitDisplays();
    }

    void ClearUnitDisplays() {
        for (int i = unitDisplays.Count-1; i >= 0; i--) {
            Destroy(unitDisplays[i].gameObject);
        }
        
        unitDisplays.Clear();
        showSingleObject.transform.DeleteAllChildren();
    }


    void SetAndStretchToParentSize(GameObject target) {
        var _mRect = target.GetComponent<RectTransform>();
        _mRect.anchorMin = Vector2.zero;
        _mRect.anchorMax = Vector2.one;
        _mRect.pivot = new Vector2(0, 0.5f);
        _mRect.sizeDelta = Vector2.zero;
        _mRect.anchoredPosition = Vector2.zero;
    }
}
