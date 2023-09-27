using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackSwitchHex : MonoBehaviour {


    public Transform topRotationHexParentPos;
    public Transform bottomRotationHexParentPos;

    public bool isRealSwitch = true;
    public Transform attachedHex;

    private float multiplier = 0.6865f; // I have no idea where this number comes from but it works.
    
    public bool isGoingLeft = false;

    public GameObject directionArrow;

    public HexTrackSegment segmentA;
    public HexTrackSegment segmentB;

    public float trackSwitchLength = 25;

    public bool hasCrossed = false;

    public float GetSwitchDistance() {
        return transform.position.z + SpeedController.s.currentDistance;
    }
    
    public void SetUp() {
        PathSelectorController.s.RegisterTrackSwitchHex(this);
    }

    private float prevAngle = 0;
    void Update() {
        if (!isRealSwitch)
            return;
        var gridSizeX = HexGrid.s.gridSize.x;
        var zPosReal = transform.position.z/ gridSizeX;
        var zPos = Mathf.Clamp(zPosReal, -0.5f, 0.5f);
        
        directionArrow.transform.rotation = Quaternion.Euler(0, !isGoingLeft? 180: 0,0);
        if (!isGoingLeft) {
            directionArrow.GetComponent<MeshRenderer>().material.color = LevelReferences.s.leftColor;
        } else {
            directionArrow.GetComponent<MeshRenderer>().material.color = LevelReferences.s.rightColor;
        }

        bool hasRotatedAtAll = false;
        if (zPos < 0.5f) {
            var startPos = transform.position.z;
            hasRotatedAtAll = Train.s.UpdateTrainCartsBasedOnRotation(startPos, startPos + gridSizeX/2f, gridSizeX*multiplier, gridSizeX/2f, isGoingLeft);
        }

        if (!hasCrossed) {
            if (zPos < 0 || hasRotatedAtAll) {
                PathSelectorController.s.DoCrossSwitch();
                hasCrossed = true;
            }
        }
        
        if (zPos < 0) {
            var angle = 90 * zPos;
            transform.rotation = Quaternion.Euler(0,angle * (isGoingLeft ? -1 : 1), 0);
            var pos = transform.position;

            pos.x = 1-Mathf.Cos(angle * Mathf.Deg2Rad);
            //print($"{pos.x}");
            pos.x *= (gridSizeX*multiplier *(isGoingLeft ? -1 : 1));
            

            transform.position = pos;
            
            CameraController.s.ManualRotateDirectControl(angle-prevAngle);
            prevAngle = angle;
        }

        if (zPosReal <= -1f) {
            enabled = false;
        }
    }


    public void AttachHex(Transform toAttach, Transform toAttach2) {
        attachedHex = toAttach;
        
        toAttach.SetParent(bottomRotationHexParentPos);
        toAttach.localPosition = Vector3.forward * HexGrid.s.gridOffset * (bottomRotationHexParentPos.childCount - 1);
        toAttach.localRotation = Quaternion.identity;

        toAttach2.SetParent(topRotationHexParentPos);
        toAttach2.localPosition = Vector3.forward * HexGrid.s.gridOffset * (bottomRotationHexParentPos.childCount - 1);
        toAttach2.localRotation = Quaternion.identity;
    }
}
