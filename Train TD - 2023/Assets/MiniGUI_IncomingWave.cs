using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGUI_IncomingWave : MonoBehaviour {
    public Vector3 sourcePosition;
    private RectTransform CanvasRect;
    private RectTransform ParentRect;
    private RectTransform UIRect;
    private Camera mainCam;

    public EnemyWave myWave;

    public float distance;

    public Image mainImage;
    public Image gunImage;
    public TMP_Text countText;
    public TMP_Text distanceText;
    
    public void SetUp(EnemyWave enemyWave) {
        myWave = enemyWave;
        sourcePosition = enemyWave.transform.position;
        sourcePosition.z = Mathf.Clamp(sourcePosition.z, -5, 5);
        CanvasRect = transform.root.GetComponent<RectTransform>();
        UIRect = GetComponent<RectTransform>();
        ParentRect = transform.parent.GetComponent<RectTransform>();
        mainCam = LevelReferences.s.mainCam;
        //counter = myWave.myData.headsUpTime;

        mainImage.sprite = enemyWave.GetMainSprite();
        var gunSprite = enemyWave.GetGunSprite();
        if (gunSprite != null) {
            gunImage.sprite = gunSprite;
        } else {
            gunImage.enabled = false;
        }

        //countText.text = (myWave.myEnemy.enemyCount != -1) ? "x" + myWave.myEnemy.enemyCount.ToString() : "x1";
        countText.text = "##";
        Update();

        GetComponent<Image>().color = enemyWave.isLeft ? LevelReferences.s.leftColor : LevelReferences.s.rightColor;
    }

    const float edgeGive = 5;
    private void Update() {
        distance = myWave.distance;
        SetPosition();
        UpdatePositionText();
    }

    private void _LateUpdate() {
        AdjustPositionBasedOnOtherPositions();
    }

    private void SetPosition() {
        //then you calculate the position of the UI element
        //0,0 for the canvas is at the center of the screen, whereas WorldToViewPortPoint
        //treats the lower left corner as 0,0. Because of this, you need to subtract the height / width of the canvas * 0.5 to get the correct position.
        Vector3 ViewportPosition = mainCam.WorldToViewportPoint(sourcePosition);

        if (ViewportPosition.z > 0) {
            Vector2 WorldObject_ScreenPosition = new Vector2(
                ((ViewportPosition.x * CanvasRect.sizeDelta.x) - (CanvasRect.sizeDelta.x * 0.5f)),
                ((ViewportPosition.y * CanvasRect.sizeDelta.y) - (CanvasRect.sizeDelta.y * 0.5f)));

            //now you can set the position of the ui element
            var halfWidthLimit = (CanvasRect.rect.width - (UIRect.rect.width + edgeGive)) / 2f;
            var halfHeightLimit = (CanvasRect.rect.height - (UIRect.rect.height + edgeGive)) / 2f;
            WorldObject_ScreenPosition.x = Mathf.Clamp(WorldObject_ScreenPosition.x,
                -halfWidthLimit,
                halfWidthLimit - (0.1f * CanvasRect.rect.width)
            );
            WorldObject_ScreenPosition.y = Mathf.Clamp(WorldObject_ScreenPosition.y,
                -halfHeightLimit + (0.2f * CanvasRect.rect.height),
                halfHeightLimit - (0.15f * CanvasRect.rect.height)
            );

            myVecRef.vector2 = WorldObject_ScreenPosition;
            UIRect.anchoredPosition = WorldObject_ScreenPosition;
        } else {
            // if we cant see the object go to some off screen location
            myVecRef.vector2 = new Vector2(100000, 100000);
            UIRect.anchoredPosition = new Vector2(100000, 100000);
        }
    }

    class Vector2Reference {
        public Vector2 vector2;
    }
    
    static List<Vector2Reference> globalPositions = new List<Vector2Reference>();
    private Vector2Reference myVecRef;
    
    private void OnEnable() {
        myVecRef = new Vector2Reference();
        globalPositions.Add(myVecRef);
        
        
        CameraController.s.AfterCameraPosUpdate.AddListener(_LateUpdate);
    }

    private void OnDisable() {
        globalPositions.Remove(myVecRef);
        PointerExit();
        
        
        CameraController.s.AfterCameraPosUpdate.RemoveListener(_LateUpdate);
    }

    void AdjustPositionBasedOnOtherPositions() {
        for (int i = 0; i < globalPositions.Count; i++) {
            var curPos = globalPositions[i];
            if (curPos != myVecRef) {
                if (Vector2.Distance(curPos.vector2, myVecRef.vector2) < UIRect.sizeDelta.x) {
                    myVecRef.vector2.x += UIRect.sizeDelta.x;
                }
            }
        }

        Vector2 adjustedPosition = myVecRef.vector2;

        UIRect.anchoredPosition = adjustedPosition;
    }

    void UpdatePositionText() {
        //text.text = myWave.myEnemy.enemyUniqueName+ ((myWave.myEnemy.enemyCount != -1) ?  " " + myWave.myEnemy.enemyCount.ToString() : "") + "\n" + distance.ToString("F1");
        distanceText.text = distance.ToString("F0") + "m";
    }


    public void PointerEnter() {
        myWave.ShowPath();
    }

    public void PointerExit() {
        myWave.HidePath();
    }
}
