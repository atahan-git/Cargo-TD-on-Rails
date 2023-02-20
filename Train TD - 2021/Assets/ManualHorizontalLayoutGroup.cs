using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualHorizontalLayoutGroup : MonoBehaviour {

    public ManualLayoutElement[] children;
    public MiniGUI_TrackPath[] paths;

    private RectTransform _rectTransform;
    public bool isDirty = false;
    public bool isLocationsDirty = false;

    private float uiSizeMultiplier => DistanceAndEnemyRadarController.s.UISizeMultiplier;
    
    void ChildrenDirty() {
        _rectTransform = GetComponent<RectTransform>();
        children = new ManualLayoutElement[transform.childCount];
        for (int i = 0; i < transform.childCount; i++) {
            children[i] = transform.GetChild(i).GetComponent<ManualLayoutElement>();
        }

        paths = GetComponentsInChildren<MiniGUI_TrackPath>();
        UpdateWidths();
        isDirty = false;
    }

    private void Update() {
        if (isDirty) {
            ChildrenDirty();
        }
        
        
        UpdateLocations();
        for (int i = 0; i < paths.Length; i++) {
            if (paths[i].widthDirty) {
                isLocationsDirty = true;
                UpdateWidths();
                return;
            } else {
                isLocationsDirty = false;
            }
        }
    }

    void UpdateLocations() {
        var percentage = 0f;

        var distanceAdjustment = SpeedController.s.currentDistance - 
                                 Mathf.Min(DistanceAndEnemyRadarController.s.playerTrainCurrentLocation, DistanceAndEnemyRadarController.s.playerTrainStaticLocation);
        distanceAdjustment *= uiSizeMultiplier;
        for (int i = 0; i < children.Length; i++) {
            var distance = percentage /* * totalLength*/;
            distance -= distanceAdjustment;
            if (children[i] == null) {
                isDirty = true;
                return;
            }
            
            var rect = children[i].GetComponent<RectTransform>();
            if (rect == null) {
                isDirty = true;
                return;
            }
            rect.anchoredPosition = new Vector2(distance, 0);
            if (children[i].isMinWidthMode) {
                percentage += children[i].minWidth;
            }else
            {
                percentage += children[i].preferredWidth*uiSizeMultiplier;
            }
        }
    }


    void UpdateWidths() {
        /*var totalPreferredWidth = 0f;

        for (int i = 0; i < paths.Length; i++) {
            if (paths[i] == null) {
                isDirty = true;
                return;
            }
            totalPreferredWidth += paths[i].GetComponent<ManualLayoutElement>().preferredWidth;
        }

        var totalLength = _rectTransform.rect.width;
        var remainingAreaAfterMinWidths = totalLength;

        for (int i = 0; i < children.Length; i++) {
            remainingAreaAfterMinWidths -= children[i].minWidth;
        }

        var pathLengthMultiplier = remainingAreaAfterMinWidths / totalPreferredWidth;*/


        /*var percentage = 0f;
        for (int i = 0; i < children.Length; i++) {
            var distance = percentage * totalLength;
            children[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(distance, 0);
            if (children[i].isMinWidthMode) {
                percentage += children[i].minWidth/totalLength;
                children[i].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, children[i].minWidth);
            }else
            {
                percentage += (children[i].preferredWidth * pathLengthMultiplier)/totalLength;
                children[i].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, children[i].preferredWidth*pathLengthMultiplier);
            }
        }*/
        
        
        var percentage = 0f;
        for (int i = 0; i < children.Length; i++) {
            /*var distance = percentage /* * totalLength#1#;
            distance -= SpeedController.s.currentDistance - DistanceAndEnemyRadarController.s.playerTrainCurrentLocation;*/
            
            if (children[i] == null) {
                isDirty = true;
                return;
            }
            
            var rect = children[i].GetComponent<RectTransform>();
            if (rect == null) {
                isDirty = true;
                return;
            }
            
            if (children[i].isMinWidthMode) {
                percentage += children[i].minWidth;
                children[i].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, children[i].minWidth);
            }else
            {
                percentage += children[i].preferredWidth*uiSizeMultiplier;
                children[i].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, children[i].preferredWidth*uiSizeMultiplier);
            }
        }

    }
}
