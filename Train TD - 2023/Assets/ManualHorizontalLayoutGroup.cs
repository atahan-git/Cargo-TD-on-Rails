using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ManualHorizontalLayoutGroup : MonoBehaviour {

    public ManualLayoutElement[] children;
    public MiniGUI_TrackPath[] paths;

    private RectTransform _rectTransform;
    public bool isDirty = false;
    public bool isLocationsDirty = false;

    public bool fitToSize = false;

    private float uiSizeMultiplier => DistanceAndEnemyRadarController.s.UISizeMultiplier;
    public float actualUISizeMultiplier;
    
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

    [Button]
    void UpdateLocations() {
        var percentage = 0f;

        /*var distanceAdjustment = SpeedController.s.currentDistance - 
                                 Mathf.Min(DistanceAndEnemyRadarController.s.playerTrainCurrentLocation, DistanceAndEnemyRadarController.s.playerTrainStaticLocation);
        distanceAdjustment *= uiSizeMultiplier;*/

        var distanceAdjustment = 0;
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
                percentage += children[i].preferredWidth*actualUISizeMultiplier;
            }
        }
    }


    void UpdateWidths() {
        var percentage = 0f;
        
        for (int i = 0; i < children.Length; i++) {
            if (children[i] == null) {
                isDirty = true;
                return;
            }
        }
        

        actualUISizeMultiplier = uiSizeMultiplier;
        if (fitToSize) {
            var totalMinWidth = 0f;
            var totalPreferredWidth = 0f;
            for (int i = 0; i < children.Length; i++) {
                if (children[i].isMinWidthMode) {
                    totalMinWidth += children[i].minWidth;
                } else {
                    totalPreferredWidth += children[i].preferredWidth;
                }
            }

            var totalAvailableWidth = _rectTransform.rect.width;
            var availableForPreferredWidth = totalAvailableWidth - totalMinWidth;
            actualUISizeMultiplier = availableForPreferredWidth / totalPreferredWidth;
        }
        
        for (int i = 0; i < children.Length; i++) {
            
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
                percentage += children[i].preferredWidth*actualUISizeMultiplier;
                children[i].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, children[i].preferredWidth*actualUISizeMultiplier);
            }
        }

    }
}
