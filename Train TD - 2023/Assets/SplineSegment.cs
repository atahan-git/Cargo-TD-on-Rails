using System.Collections;
using System.Collections.Generic;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;

public class SplineSegment : MonoBehaviour {

    public SplineSegment pathA;
    public SplineSegment pathB;

    public SplineFloorGenerator left;
    public SplineFloorGenerator right;
    

    public SplineComputer splineComputer => GetComponent<SplineComputer>();

    private const float yVal = 0.353f;
    [Button]
    public void MakeSegment(float length, float flatSection = 0) {
        left.flatSectionLength = flatSection;
        left.flatSectionTransition = 6;
        right.flatSectionLength = flatSection;
        right.flatSectionTransition = 6;
        
        var splinePoints = new SplinePoint[2];
        
        var point = new SplinePoint(new Vector3(0, yVal, 0));
        point.tangent = new Vector3(0, yVal, -5);
        point.tangent2 = new Vector3(0, yVal, 5);
        splinePoints[0] = point;
        
        
        point = new SplinePoint(new Vector3(0, yVal, length));
        point.tangent = new Vector3(0, yVal, length-5);
        point.tangent2 = new Vector3(0, yVal, length+5);
        splinePoints[1] = point;
        
        splineComputer.SetPoints(splinePoints);
        splineComputer.sampleRate = Mathf.CeilToInt(length);
        //splineComputer.Rebuild();
        StartCoroutine(BuildNextFrame(splineComputer));
        /*foreach (var user in GetComponentsInChildren<SplineUser>()) {
            user.RebuildImmediate();
        }*/
    }

    IEnumerator BuildNextFrame(SplineComputer computer) {
        yield return null;
        computer.RebuildImmediate();
        //computer.
    }
    
    
    [Button]
    public void MakeSwitchSegment(float length, bool rotateToTheRight) {
        left.flatSectionLength = 0;
        left.flatSectionTransition = 0;
        right.flatSectionLength = 0;
        right.flatSectionTransition = 0;

        if (rotateToTheRight) {
            right.flatSectionLength = 15;
            right.flatSectionTransition = 6;
        } else {
            left.flatSectionLength = 15;
            left.flatSectionTransition = 6;
        }
        
        var splinePoints = new SplinePoint[3];
        
        
        var point = new SplinePoint(new Vector3(0, yVal, 0));
        point.tangent = new Vector3(0, yVal, -10);
        point.tangent2 = new Vector3(0, yVal, 10);
        splinePoints[0] = point;


        var angle = 30;
        var bigAngle = angle + 15;
        if (rotateToTheRight) {
            angle = -angle;
            bigAngle = -bigAngle;
        }
        var radius = 30;

        var pointX = Mathf.Sin(Mathf.Deg2Rad*angle)*radius;
        var pointY = Mathf.Cos(Mathf.Deg2Rad*angle)*radius;

        Quaternion rotation = Quaternion.AngleAxis( bigAngle, Vector3.up);
        var forwardVector =rotation* Vector3.forward ;

        var startPoint = new Vector3(pointX, yVal, pointY);
        point = new SplinePoint(startPoint);
        point.tangent = startPoint - (forwardVector*10);
        point.tangent2 = startPoint + (forwardVector*10);
        splinePoints[1] = point;


        var endPoint = startPoint + forwardVector * length;
        point = new SplinePoint(endPoint);
        point.tangent = endPoint  - (forwardVector*10);
        point.tangent2 = endPoint + (forwardVector*10);
        splinePoints[2] = point;
        
        splineComputer.SetPoints(splinePoints);
        print((Mathf.PI * (Mathf.Deg2Rad * angle)));
        splineComputer.sampleRate = Mathf.CeilToInt((length + (Mathf.PI * (Mathf.Deg2Rad * angle)*15) )/2);
        splineComputer.RebuildImmediate();
    }


    [Button]
    public void AttachToSegment(SplineSegment segment) { // attach to the very end
        var endPoint = segment.splineComputer.Evaluate(segment.splineComputer.pointCount-1);
        //print(endPoint.position);
        transform.position = endPoint.position;
        transform.rotation = Quaternion.LookRotation(endPoint.forward);
    }
}
