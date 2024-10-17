using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIElementFollowWorldTarget : MonoBehaviour {

    public bool autoSetUp = false;
    public bool avoidOverlaps = true;

     bool transformMode = true;
    public Transform SetUp(Transform target) {
        transformMode = true;
        sourceTransform = target;
        
        CanvasRect = transform.root.GetComponent<RectTransform>();
        UIRect = GetComponent<RectTransform>();
        ParentRect = transform.parent.GetComponent<RectTransform>();
        mainCam = LevelReferences.s.mainCam;

        this.enabled = true;
        return transform;
    }
    
    public void UpdateTarget(Vector3 location) {
        transformMode = false;
        sourceLocation = location;
        
        CanvasRect = transform.root.GetComponent<RectTransform>();
        UIRect = GetComponent<RectTransform>();
        ParentRect = transform.parent.GetComponent<RectTransform>();
        mainCam = LevelReferences.s.mainCam;

        this.enabled = true;
    }
    
    
    public Transform sourceTransform;
    public Vector3 sourceLocation;
    private RectTransform CanvasRect;
    private RectTransform ParentRect;
    [HideInInspector]
    public RectTransform UIRect;
    private Camera mainCam;

    /*private void Update() {
        SetPosition();
    }*/
    
    
    private void _LateUpdate() {
        if (transformMode) {
            if (sourceTransform == null) {
                this.enabled = false;
                return;
            }
        }

        SetPosition();
    }

    void SetPosition() {
        if (transformMode)
            sourceLocation = sourceTransform.position;
        
        SetPosition(sourceLocation);
    }

    public void OneTimeSetPosition(Vector3 target) {
        CanvasRect = transform.root.GetComponent<RectTransform>();
        UIRect = GetComponent<RectTransform>();
        ParentRect = transform.parent.GetComponent<RectTransform>();
        mainCam = LevelReferences.s.mainCam;
        SetPosition(target);
    }
    void SetPosition(Vector3 target) {
        //then you calculate the position of the UI element
        //0,0 for the canvas is at the center of the screen, whereas WorldToViewPortPoint
        //treats the lower left corner as 0,0. Because of this, you need to subtract the height / width of the canvas * 0.5 to get the correct position.
        //SetOffset(); // for debugging
        
        Vector3 ViewportPosition = mainCam.WorldToViewportPoint(target);

        if (ViewportPosition.z > 0) {
            // if the object is within our view
            Vector2 WorldObject_ScreenPosition = new Vector2(
                ((ViewportPosition.x * CanvasRect.sizeDelta.x) - (CanvasRect.sizeDelta.x * 0.5f)),
                ((ViewportPosition.y * CanvasRect.sizeDelta.y) - (CanvasRect.sizeDelta.y * 0.5f)));

            /*//now you can set the position of the ui element
            var halfWidthLimit = (CanvasRect.rect.width - (UIRect.rect.width + edgeGive)) / 2f;
            var halfHeightLimit = (CanvasRect.rect.height - (UIRect.rect.height + edgeGive)) / 2f;
            WorldObject_ScreenPosition.x = Mathf.Clamp(WorldObject_ScreenPosition.x,
                -halfWidthLimit,
                halfWidthLimit
            );
            WorldObject_ScreenPosition.y = Mathf.Clamp(WorldObject_ScreenPosition.y,
                -halfHeightLimit + (0.1f * CanvasRect.rect.height),
                halfHeightLimit
            );*/

            if (PlayerWorldInteractionController.s.screenIsFlipped) {
                WorldObject_ScreenPosition.x = CanvasRect.sizeDelta.x - WorldObject_ScreenPosition.x;
            }

            UIRect.anchoredPosition = WorldObject_ScreenPosition;
        } else {
            // if we cant see the object go to some off screen location
            UIRect.anchoredPosition = new Vector2(100000, 100000);
        }
    }
    
    

    private void OnEnable() {
        this.enabled = sourceTransform != null;
        if (autoSetUp)
            SetUp(sourceTransform);
        
        UIWorldFollowerSorter.activeElements.Add(this);
        if(avoidOverlaps)
            UIWorldFollowerSorter.avoidanceElements.Add(this);
        
        CameraController.s.AfterCameraPosUpdate.AddListener(_LateUpdate);
    }

    private void OnDisable() {
        UIWorldFollowerSorter.activeElements.Remove(this);
        if(avoidOverlaps)
            UIWorldFollowerSorter.avoidanceElements.Remove(this);
        
        if(CameraController.s != null)
            CameraController.s.AfterCameraPosUpdate.RemoveListener(_LateUpdate);
    }
}
