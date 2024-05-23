using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugFollowPathDistance : MonoBehaviour
{
    public float myDistance;

    public bool followTrain = false;
    public void LateUpdate() {
        if (followTrain) {
            transform.position = Train.s.trainMiddle.position;
            return;
        }
            
        
        transform.position = PathAndTerrainGenerator.s.GetPointOnActivePath(myDistance-SpeedController.s.currentDistance) ;
        transform.rotation = PathAndTerrainGenerator.s.GetRotationOnActivePath(myDistance-SpeedController.s.currentDistance) /** Quaternion.Euler(0,180,0)*/;
    }
}
