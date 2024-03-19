using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class InfiniteRampartsMaker : MonoBehaviour {

    public GameObject prefab;
    
    public Transform lastPosObj;
    public int curCount = 0;
    public int maxCount = 20;
    
    [Button]
    void DebugMakeInfiniteRamparts(int count = 3) {
        transform.DeleteAllChildrenEditor();

        curCount = 0;
        lastPosObj = MakeLastPos();

        for (int i = 0; i < count; i++) {
            var result = MakeSingleRampart();
        }
    }

    private void OnDisable() {
        if (PathAndTerrainGenerator.s != null) {
            PathAndTerrainGenerator.s.OnNewTerrainStabilized.RemoveListener(MakeInfiniteRamparts);
        }

        if (PlayStateMaster.s != null) {
            PlayStateMaster.s.OnDrawWorld.RemoveListener(ResetStartPos);
        }
    }

    private void OnEnable() {
        ResetStartPos();
        if (PathAndTerrainGenerator.s != null && PlayStateMaster.s != null) {
            Register();
        } else {
            Invoke(nameof(Register),0.01f);
        }
    }

    void Register() {
        PathAndTerrainGenerator.s.OnNewTerrainStabilized.AddListener(MakeInfiniteRamparts);
        PlayStateMaster.s.OnDrawWorld.AddListener(ResetStartPos);

        if (PathAndTerrainGenerator.s.initialTerrainMade) {
            MakeInfiniteRamparts();
        }
    }

    void ResetStartPos() {
        //print("reset start pos");
        curCount = 0;
        transform.DeleteAllChildren();
        lastPosObj = MakeLastPos();
    }
    
    void MakeInfiniteRamparts() {
        //Debug.Log("make ramparts");
        if (!makingRampart) {
            StartCoroutine(_MakeInfiniteRamparts());
        }
    }

    Transform MakeLastPos() {
        var myObj = new GameObject("Last pos");
        myObj.transform.position = transform.position;
        myObj.transform.rotation = transform.rotation;
        myObj.transform.SetParent(transform);

        return myObj.transform;
    }

    private bool makingRampart = false;
    IEnumerator _MakeInfiniteRamparts() {
        makingRampart = true;
        //Debug.Log($"New rampart starter {lastPosObj.transform.position}");
        var n = 0;
        var legalRampart = true;
        while (legalRampart && curCount <= maxCount) {
            legalRampart = MakeSingleRampart();
            //Debug.Log($"Rampart State : {legalRampart} - {lastPosObj.transform.position}");
            yield return null;
            n++;
            
            if(n > 10)
                break;
        }

        makingRampart = false;
    }


    public float rampantXSize = 6.562802f;
    public float rampantYSize = 6.562802f;
    private int pointCount = 6;

    public LayerMask debugLayerMask;
    bool MakeSingleRampart() {
        var rotAngle = Random.Range(-10, 10);
        lastPosObj.Rotate(0, rotAngle, 0 );
        
        var lastDir = lastPosObj.forward;
        var lastPos = lastPosObj.position;
        
        
        var points = new List<Vector3>();

        var stepLength = rampantXSize / (pointCount-1);
        var layerMask = LevelReferences.s.groundLayer;
        //var layerMask = debugLayerMask;

        TrainTerrainData hitTerrain = null;
        
        points.Add(lastPos);
        
        for (int i = 1; i < pointCount; i++) {
            var castPos = lastPos +  i * stepLength * lastDir + Vector3.up*10;

            if (Physics.Raycast(castPos, Vector3.down, out RaycastHit hit, 40, layerMask)) {
                points.Add(hit.point);
                
                //Debug.DrawLine(hit.point, hit.point+Vector3.up*5, Color.red, 4f);

                if (hitTerrain == null) {
                    hitTerrain = hit.collider.GetComponent<TrainTerrainData>();
                }
            }
        }

        if (hitTerrain == null || points.Count <= 1) {
            lastPosObj.Rotate(0, -rotAngle, 0);
            return false;
        }

        var directionVector = FindDirectionWithBiggestAngle(points,lastDir);
      
        var angle = Vector3.Angle(lastDir, directionVector);
        var maxAngle = 20;
        if (angle > maxAngle) {
            var cross = Vector3.Cross(lastDir, directionVector);
            directionVector = Quaternion.AngleAxis( maxAngle - angle, cross) * directionVector;
        }

        var rampart = Instantiate(prefab, transform);
        rampart.transform.rotation = Quaternion.LookRotation(directionVector, Vector3.up);
        //rampart.transform.Rotate(0, -90,0);
        //Debug.DrawLine(lastPos, lastPos+(directionVector*rampantXSize), Color.blue, 2f, false);

        var startPos = Vector3.MoveTowards(lastPos, points[1], 0.1f);

        rampart.transform.position = startPos;

        hitTerrain.AddForeignObject(rampart);

        ShearWithTransforms.ResizeObject(rampart, rampantXSize, rampantYSize);

        lastPos = startPos + directionVector * rampantXSize;

        lastPosObj.transform.position = lastPos;

        curCount += 1;
        return true;
    }
    

    Vector3 FindDirectionWithBiggestAngle(List<Vector3> points, Vector3 forward) {
        var minAngle = 0f;
        var startPoint = points[0];
        var endPoint = points[^1];

        for (int i = 1; i < points.Count; i++) {
            var directionVector = (points[i] - startPoint).normalized;
            var angle = Vector3.Angle(forward, directionVector);
            //Debug.DrawLine(points[i], points[i]+forward*5, Color.yellow,1f, false);
            //Debug.DrawLine(points[i], points[i]+directionVector*5, Color.green,1f, false);

            if (directionVector.y < forward.y)
                angle = -angle;
            
            //print(angle);

            if (angle < minAngle) {
                minAngle = angle;
                endPoint = points[i];
            }
        }

        return (endPoint - startPoint).normalized;
    }
}
